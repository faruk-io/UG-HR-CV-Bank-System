import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, formatDate } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { CandidateService } from '../../core/candidate.service';
import { SettingsService } from '../../core/settings.service';
import { CandidateEvaluationHistoryItem, interviewDecisionLabel, interviewStatusLabel } from '../../core/evaluation.models';
import { EvaluationService } from '../../core/evaluation.service';
import {
  CANDIDATE_STATUSES,
  DOCUMENT_CATEGORIES,
  Candidate,
  CandidateDocument,
  formatBytes,
  statusLabel
} from '../../core/candidate.models';

import { Icon } from '../../layout/icon/icon';

@Component({
  selector: 'app-candidate-detail',
  imports: [CommonModule, FormsModule, RouterLink, Icon],
  templateUrl: './candidate-detail.html',
  styleUrl: './candidate-detail.scss'
})
export class CandidateDetail implements OnInit {
  protected readonly auth = inject(AuthService);
  protected readonly settings = inject(SettingsService);
  private readonly service = inject(CandidateService);
  private readonly evaluations = inject(EvaluationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly candidate = signal<Candidate | null>(null);
  protected readonly loading = signal(false);
  protected readonly history = signal<CandidateEvaluationHistoryItem[]>([]);
  protected readonly error = signal('');
  protected readonly notice = signal('');
  protected readonly statuses = CANDIDATE_STATUSES;
  protected readonly categories = DOCUMENT_CATEGORIES;
  protected readonly formatBytes = formatBytes;

  protected readonly file = signal<File | null>(null);
  protected readonly category = signal<number>(DOCUMENT_CATEGORIES.cv);
  protected readonly remarks = signal('');
  protected readonly replaceCurrent = signal(false);
  protected readonly uploading = signal(false);

  protected readonly statusLabel = interviewStatusLabel;
  protected readonly decisionLabel = interviewDecisionLabel;

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.load(id);

    const cvUploadError = (window.history.state as { cvUploadError?: string } | null)?.cvUploadError;
    if (cvUploadError) {
      this.error.set(`The candidate was saved, but the CV upload failed: ${cvUploadError} You can upload the CV below.`);
    }

    if (this.auth.can('evaluation.view')) {
      this.evaluations.history(id).subscribe({
        next: items => this.history.set(items),
        error: () => this.history.set([])
      });
    }
  }

  protected finalLine(row: CandidateEvaluationHistoryItem): string {
    const parts = [
      row.fixedDesignation,
      row.fixedSalary === null ? null : this.money(row.fixedSalary),
      row.grade ? `Grade ${row.grade}` : null,
      row.dateOfJoining ? `DOJ ${this.shortDate(row.dateOfJoining)}` : null
    ].filter((part): part is string => !!part);

    return parts.length ? parts.join(' · ') : '—';
  }

  protected money(value: number | null): string {
    return value === null ? '—' : value.toLocaleString('en-US', { maximumFractionDigits: 0 });
  }

  protected load(id: number): void {
    this.loading.set(true);
    this.error.set('');

    this.service.get(id).subscribe({
      next: candidate => {
        this.candidate.set(candidate);
        this.loading.set(false);
      },
      error: error => {
        this.error.set(this.service.errorMessage(error, 'Could not load this candidate.'));
        this.loading.set(false);
      }
    });
  }

  protected get label(): string {
    const candidate = this.candidate();
    return candidate ? statusLabel(candidate.status) : '';
  }

  protected onFilePicked(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.file.set(input.files?.[0] ?? null);
  }

  protected upload(): void {
    const candidate = this.candidate();
    const file = this.file();

    if (!candidate || !file) {
      this.error.set('Choose a PDF, DOC or DOCX file first.');
      return;
    }

    this.uploading.set(true);
    this.error.set('');
    this.notice.set('');

    this.service
      .uploadDocument(candidate.id, file, this.category(), this.remarks().trim() || null, this.replaceCurrent())
      .subscribe({
        next: () => {
          this.uploading.set(false);
          this.notice.set(`${file.name} uploaded successfully.`);
          this.file.set(null);
          this.remarks.set('');
          this.replaceCurrent.set(false);
          const input = document.getElementById('cv-file') as HTMLInputElement | null;
          if (input) {
            input.value = '';
          }
          this.load(candidate.id);
        },
        error: error => {
          this.uploading.set(false);
          this.error.set(this.service.errorMessage(error, 'The file was not accepted.'));
        }
      });
  }

  protected view(document: CandidateDocument): void {
    this.service.fetchDocument(document.id, true).subscribe({
      next: blob => this.openBlob(blob, document.fileName, false),
      error: error => this.error.set(this.service.errorMessage(error, 'Could not open the file.'))
    });
  }

  protected download(document: CandidateDocument): void {
    this.service.fetchDocument(document.id, false).subscribe({
      next: blob => this.openBlob(blob, document.fileName, true),
      error: error => this.error.set(this.service.errorMessage(error, 'Could not download the file.'))
    });
  }

  protected withdraw(document: CandidateDocument): void {
    const candidate = this.candidate();

    if (!candidate) {
      return;
    }

    if (!confirm(`Withdraw "${document.fileName}"? The record stays in the history as withdrawn.`)) {
      return;
    }

    this.service.withdrawDocument(document.id).subscribe({
      next: () => {
        this.notice.set(`${document.fileName} was withdrawn.`);
        this.load(candidate.id);
      },
      error: error => this.error.set(this.service.errorMessage(error, 'Could not withdraw the file.'))
    });
  }

  protected changeStatus(status: number): void {
    const candidate = this.candidate();

    if (!candidate || candidate.status === status) {
      return;
    }

    this.service.setStatus(candidate.id, status).subscribe({
      next: updated => {
        this.candidate.set(updated);
        this.notice.set(`Status changed to ${statusLabel(status)}.`);
      },
      error: error => this.error.set(this.service.errorMessage(error, 'Could not change the status.'))
    });
  }

  protected deleteCandidate(): void {
    const candidate = this.candidate();

    if (!candidate) {
      return;
    }

    if (!confirm(`Delete candidate ${candidate.candidateCode} (${candidate.name}) permanently?`)) {
      return;
    }

    this.service.remove(candidate.id).subscribe({
      next: () => this.router.navigate(['/candidates']),
      error: error => this.error.set(this.service.errorMessage(error, 'Could not delete the candidate.'))
    });
  }

  protected categoryLabel(category: number): string {
    if (category === DOCUMENT_CATEGORIES.cv) return 'CV';
    if (category === DOCUMENT_CATEGORIES.completedEvaluationForm) return 'Completed Evaluation Form';
    return 'Other document';
  }

  private openBlob(blob: Blob, fileName: string, download: boolean): void {
    const url = URL.createObjectURL(blob);

    if (download) {
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = fileName;
      anchor.click();
      URL.revokeObjectURL(url);
      return;
    }

    const tab = window.open(url, '_blank');

    if (!tab) {
      this.error.set('The browser blocked the preview window. Allow pop-ups for this site, or use Download.');
    }

    setTimeout(() => URL.revokeObjectURL(url), 60_000);
  }

  protected shortDate(value: string | null): string {
    return value ? formatDate(value, 'mediumDate', 'en-US') : '—';
  }
}
