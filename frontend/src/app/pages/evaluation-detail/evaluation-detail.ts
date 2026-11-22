import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { CandidateService } from '../../core/candidate.service';
import { MasterDataOption, formatBytes } from '../../core/candidate.models';
import { EvaluationService } from '../../core/evaluation.service';
import { SettingsService } from '../../core/settings.service';
import { Icon } from '../../layout/icon/icon';
import {
  APPROVAL_DECISIONS,
  Evaluation,
  EvaluationSaveRequest,
  INTERVIEW_DECISIONS,
  INTERVIEW_STATUSES,
  RATING_CHOICES,
  interviewDecisionLabel
} from '../../core/evaluation.models';

interface DraftDocumentItem {
  itemCode: number;
  itemName: string;
  details: string | null;
  copySubmitted: boolean;
  originalChecked: boolean;
}

interface DraftRating {
  dimensionCode: number;
  dimensionName: string;
  rating: number;
}

interface Draft {
  interviewNumber: number | null;
  interviewDate: string;
  status: number;
  appliedJobPositionId: number | null;
  concernId: number | null;
  personsInterviewTaken: number | null;
  remarks: string | null;
  ratings: DraftRating[];
  documentItems: DraftDocumentItem[];
  references: { referenceNumber: number; referenceName: string | null; designation: string | null; relationship: string | null; companyAndLocation: string | null; phone: string | null; statement: string | null }[];
  evaluators: { sectionNumber: number; findings: string | null; evaluatorName: string | null; designation: string | null; department: string | null; signatureText: string | null }[];
  result: {
    decision: number;
    recommendation: string | null;
    expectedSalary: number | null;
    noticePeriod: string | null;
    designationFixedId: number | null;
    salaryFixed: number | null;
    grade: string | null;
    dateOfJoining: string | null;
    remarks: string | null;
  };
  approvals: { authorityNumber: number; authorityTitle: string; decision: string | null; signatureText: string | null }[];
}

function datePart(value: string | null): string | null {
  return value ? value.slice(0, 10) : null;
}

@Component({
  selector: 'app-evaluation-detail',
  imports: [CommonModule, FormsModule, RouterLink, Icon],
  templateUrl: './evaluation-detail.html',
  styleUrl: './evaluation-detail.scss'
})
export class EvaluationDetail implements OnInit {
  protected readonly auth = inject(AuthService);
  protected readonly settings = inject(SettingsService);
  private readonly service = inject(EvaluationService);
  private readonly master = inject(CandidateService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly evaluation = signal<Evaluation | null>(null);
  protected readonly draft = signal<Draft | null>(null);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly error = signal('');
  protected readonly notice = signal('');

  protected readonly jobPositions = signal<MasterDataOption[]>([]);
  protected readonly concerns = signal<MasterDataOption[]>([]);
  protected readonly designations = signal<MasterDataOption[]>([]);

  protected readonly statuses = INTERVIEW_STATUSES;
  protected readonly decisions = INTERVIEW_DECISIONS;
  protected readonly ratingChoices = RATING_CHOICES;
  protected readonly approvalDecisions = APPROVAL_DECISIONS;

  protected readonly totalRating = computed(() =>
    (this.draft()?.ratings ?? []).reduce((sum, row) => sum + (row.rating || 0), 0)
  );

  protected readonly maxRating = computed(() => this.evaluation()?.maxRating ?? 50);

  protected readonly isOwner = computed(() => {
    const user = this.auth.currentUser();
    const evaluation = this.evaluation();
    return !!user && !!evaluation && evaluation.createdById === user.userId;
  });

  protected readonly canEdit = computed(
    () => this.auth.can('evaluation.generate') && (this.isOwner() || this.auth.can('evaluation.manage'))
  );

  protected readonly canUploadScan = computed(() => this.auth.can('evaluation.upload'));
  protected readonly canDelete = computed(() => this.auth.can('evaluation.manage'));

  protected scanFile: File | null = null;
  protected scanRemarks = '';

  ngOnInit(): void {
    this.master.jobPositions().subscribe(items => this.jobPositions.set(items));
    this.master.concerns().subscribe(items => this.concerns.set(items));
    this.master.designations().subscribe(items => this.designations.set(items));

    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) {
      this.error.set('This evaluation could not be opened.');
      this.loading.set(false);
      return;
    }

    this.load(id);
  }

  protected readonly decisionLabel = interviewDecisionLabel;
  protected readonly formatBytes = formatBytes;

  protected save(): void {
    const evaluation = this.evaluation();
    const draft = this.draft();
    if (!evaluation || !draft) return;

    this.saving.set(true);
    this.error.set('');
    this.notice.set('');

    this.service.update(evaluation.id, this.toRequest(draft)).subscribe({
      next: saved => {
        this.apply(saved);
        this.saving.set(false);
        this.notice.set('Evaluation saved.');
      },
      error: error => {
        this.error.set(this.service.errorMessage(error, 'Could not save this evaluation.'));
        this.saving.set(false);
      }
    });
  }

  protected onScanPicked(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.scanFile = input.files?.[0] ?? null;
  }

  protected uploadScan(): void {
    const evaluation = this.evaluation();
    if (!evaluation || !this.scanFile) {
      this.error.set('Choose the scanned form file first.');
      return;
    }

    this.saving.set(true);
    this.error.set('');

    this.service.uploadScan(evaluation.id, this.scanFile, this.scanRemarks || null).subscribe({
      next: () => {
        this.scanFile = null;
        this.scanRemarks = '';
        const input = document.getElementById('scan-file') as HTMLInputElement | null;
        if (input) input.value = '';
        this.load(evaluation.id, 'Scanned form uploaded.');
      },
      error: error => {
        this.error.set(this.service.errorMessage(error, 'Could not upload the scanned form.'));
        this.saving.set(false);
      }
    });
  }

  protected openScan(documentId: number): void {
    const evaluation = this.evaluation();
    if (!evaluation) return;

    this.service.downloadScan(documentId, true).subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        const frame = document.getElementById('scan-view') as HTMLIFrameElement | null;
        if (frame) {
          frame.src = url;
          frame.parentElement?.classList.remove('hidden');
        }
      },
      error: error => this.error.set(this.service.errorMessage(error, 'Could not open the scanned form.'))
    });
  }

  protected downloadScan(documentId: number): void {
    const evaluation = this.evaluation();
    if (!evaluation) return;

    const scan = evaluation.scannedForms.find(item => item.id === documentId);
    this.service.downloadScan(documentId, false).subscribe({
      next: blob => {
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = scan?.fileName ?? 'evaluation-form';
        link.click();
        URL.revokeObjectURL(link.href);
      },
      error: error => this.error.set(this.service.errorMessage(error, 'Could not download the scanned form.'))
    });
  }

  protected remove(): void {
    const evaluation = this.evaluation();
    if (!evaluation) return;

    if (!confirm(`Delete interview ${evaluation.interviewNumber} for ${evaluation.candidate.name}? The scanned forms are removed too.`)) {
      return;
    }

    this.saving.set(true);
    this.service.remove(evaluation.id).subscribe({
      next: () => this.router.navigate(['/evaluations']),
      error: error => {
        this.error.set(this.service.errorMessage(error, 'Could not delete this evaluation.'));
        this.saving.set(false);
      }
    });
  }

  protected touch(): void {
    this.draft.update(draft => (draft ? { ...draft } : draft));
  }

  protected addMasterData(kind: 'concern' | 'position' | 'designation'): void {
    const question =
      kind === 'concern' ? 'New concern name' : kind === 'position' ? 'New applied position title' : 'New designation name';

    const name = prompt(question);
    const value = name?.trim();

    if (!value) return;

    const request =
      kind === 'concern'
        ? this.master.addConcern(value)
        : kind === 'position'
          ? this.master.addJobPosition(value)
          : this.master.addDesignation(value);

    request.subscribe({
      next: created => {
        const sorted = (items: MasterDataOption[]) =>
          [...items, created].sort((a, b) => a.name.localeCompare(b.name));

        if (kind === 'concern') {
          this.concerns.update(sorted);
          this.draft.update(draft => (draft ? { ...draft, concernId: created.id } : draft));
        } else if (kind === 'position') {
          this.jobPositions.update(sorted);
          this.draft.update(draft => (draft ? { ...draft, appliedJobPositionId: created.id } : draft));
        } else {
          this.designations.update(sorted);
          this.draft.update(draft =>
            draft?.result ? { ...draft, result: { ...draft.result, designationFixedId: created.id } } : draft);
        }
      },
      error: error => this.error.set(this.service.errorMessage(error, 'The new entry could not be created.'))
    });
  }

  protected firstHalf(items: DraftDocumentItem[]): DraftDocumentItem[] {
    return items.filter(item => item.itemCode <= 4);
  }

  protected secondHalf(items: DraftDocumentItem[]): DraftDocumentItem[] {
    return items.filter(item => item.itemCode > 4);
  }

  protected ratingPairs(): [DraftRating[], DraftRating[]] {
    const ratings = this.draft()?.ratings ?? [];
    const half = Math.ceil(ratings.length / 2);
    return [ratings.slice(0, half), ratings.slice(half)];
  }

  private load(id: number, notice?: string): void {
    this.loading.set(true);
    this.error.set('');
    if (notice !== undefined) this.notice.set(notice);

    this.service.get(id).subscribe({
      next: evaluation => {
        this.apply(evaluation);
        this.loading.set(false);
      },
      error: error => {
        this.error.set(this.service.errorMessage(error, 'This evaluation could not be loaded.'));
        this.loading.set(false);
      }
    });
  }

  private apply(evaluation: Evaluation): void {
    this.evaluation.set(evaluation);
    this.draft.set({
      interviewNumber: evaluation.interviewNumber,
      interviewDate: datePart(evaluation.interviewDate) ?? '',
      status: evaluation.status,
      appliedJobPositionId: evaluation.appliedJobPositionId,
      concernId: evaluation.concernId,
      personsInterviewTaken: evaluation.personsInterviewTaken,
      remarks: evaluation.remarks,
      ratings: evaluation.ratings.map(r => ({ ...r })),
      documentItems: evaluation.documentItems.map(r => ({ ...r })),
      references: evaluation.references.map(r => ({ ...r })),
      evaluators: evaluation.evaluators.map(r => ({ ...r })),
      result: {
        decision: evaluation.result?.decision ?? 0,
        recommendation: evaluation.result?.recommendation ?? null,
        expectedSalary: evaluation.result?.expectedSalary ?? evaluation.candidate.expectedSalary,
        noticePeriod: evaluation.result?.noticePeriod ?? evaluation.candidate.noticePeriod,
        designationFixedId: evaluation.result?.designationFixedId ?? null,
        salaryFixed: evaluation.result?.salaryFixed ?? null,
        grade: evaluation.result?.grade ?? null,
        dateOfJoining: datePart(evaluation.result?.dateOfJoining ?? null),
        remarks: evaluation.result?.remarks ?? null
      },
      approvals: evaluation.approvals.map(r => ({
        authorityNumber: r.authorityNumber,
        authorityTitle: r.authorityTitle,
        decision: r.decision,
        signatureText: r.signatureText
      }))
    });
  }

  private toRequest(draft: Draft): EvaluationSaveRequest {
    return {
      candidateId: this.evaluation()?.candidateId ?? 0,
      interviewNumber: draft.interviewNumber,
      interviewDate: `${draft.interviewDate}T00:00:00Z`,
      status: draft.status,
      appliedJobPositionId: draft.appliedJobPositionId,
      concernId: draft.concernId,
      personsInterviewTaken: draft.personsInterviewTaken,
      remarks: draft.remarks,
      ratings: draft.ratings.map(r => ({ dimensionCode: r.dimensionCode, rating: r.rating || 0 })),
      documentItems: draft.documentItems.map(r => ({
        itemCode: r.itemCode,
        details: r.details,
        copySubmitted: r.copySubmitted,
        originalChecked: r.originalChecked
      })),
      references: draft.references.map(r => ({ ...r })),
      evaluators: draft.evaluators.map(r => ({ ...r })),
      approvals: draft.approvals.map(r => ({
        authorityNumber: r.authorityNumber,
        decision: r.decision,
        signatureText: r.signatureText
      })),
      result: {
        ...draft.result,
        dateOfJoining: draft.result.dateOfJoining ? `${draft.result.dateOfJoining}T00:00:00Z` : null
      }
    };
  }
}
