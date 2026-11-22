import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ActivatedRoute } from '@angular/router';
import { CandidateService } from '../../core/candidate.service';
import { SettingsService } from '../../core/settings.service';
import {
  CANDIDATE_STATUSES,
  DOCUMENT_CATEGORIES,
  Candidate,
  CandidateSaveRequest,
  DuplicateCandidate,
  MasterDataOption
} from '../../core/candidate.models';

import { Icon } from '../../layout/icon/icon';

@Component({
  selector: 'app-candidate-form',
  imports: [CommonModule, ReactiveFormsModule, RouterLink, Icon],
  templateUrl: './candidate-form.html',
  styleUrl: './candidate-form.scss'
})
export class CandidateForm implements OnInit {
  private readonly service = inject(CandidateService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);
  protected readonly settings = inject(SettingsService);

  protected readonly candidateId = signal<number | null>(null);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal('');
  protected readonly cvFile = signal<File | null>(null);
  protected readonly duplicates = signal<DuplicateCandidate[]>([]);
  protected readonly confirmDuplicate = signal(false);
  protected readonly jobPositions = signal<MasterDataOption[]>([]);
  protected readonly concerns = signal<MasterDataOption[]>([]);
  protected readonly statuses = CANDIDATE_STATUSES;

  protected readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    mobileNumber: ['', Validators.maxLength(30)],
    email: ['', [Validators.email, Validators.maxLength(200)]],
    dateOfBirth: [''],
    presentAddress: [''],
    currentOrganization: ['', Validators.maxLength(200)],
    currentDesignation: ['', Validators.maxLength(200)],
    durationInCurrentOrganization: ['', Validators.maxLength(100)],
    totalYearsOfExperience: [''],
    appliedJobPositionId: [''],
    concernId: [''],
    presentSalary: [''],
    expectedSalary: [''],
    noticePeriod: ['', Validators.maxLength(100)],
    remarks: ['', Validators.maxLength(1000)],
    status: ['1']
  });

  protected readonly isEdit = computed(() => this.candidateId() !== null);
  protected readonly title = computed(() => (this.isEdit() ? 'Edit Candidate' : 'Add Candidate'));

  ngOnInit(): void {
    this.settings.ensureDisplay();
    this.reloadMasterData();

    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (Number.isFinite(id) && id > 0) {
      this.candidateId.set(id);
      this.loading.set(true);
      this.service.get(id).subscribe({
        next: candidate => {
          this.patch(candidate);
          this.loading.set(false);
        },
        error: error => {
          this.error.set(this.service.errorMessage(error, 'Could not load this candidate.'));
          this.loading.set(false);
        }
      });
    }

    this.form.controls.mobileNumber.valueChanges.subscribe(() => this.checkDuplicates());
    this.form.controls.email.valueChanges.subscribe(() => this.checkDuplicates());
  }

  protected reloadMasterData(): void {
    this.service.jobPositions().subscribe(items => this.jobPositions.set(items));
    this.service.concerns().subscribe(items => this.concerns.set(items));
  }

  protected checkDuplicates(): void {
    const mobile = this.form.controls.mobileNumber.value.trim();
    const email = this.form.controls.email.value.trim();

    if (mobile.length < 4 && email.length < 4) {
      this.duplicates.set([]);
      return;
    }

    this.service.findDuplicates(mobile || null, email || null, this.candidateId() ?? undefined).subscribe({
      next: rows => {
        this.duplicates.set(rows);
        if (rows.length === 0) {
          this.confirmDuplicate.set(false);
        }
      },
      error: () => this.duplicates.set([])
    });
  }

  protected addMasterData(kind: 'concern' | 'position'): void {
    const name = prompt(kind === 'concern' ? 'New concern name' : 'New applied position title');
    const value = name?.trim();

    if (!value) {
      return;
    }

    const request = kind === 'concern' ? this.service.addConcern(value) : this.service.addJobPosition(value);

    request.subscribe({
      next: created => {
        if (kind === 'concern') {
          this.concerns.update(items => [...items, created].sort((a, b) => a.name.localeCompare(b.name)));
          this.form.controls.concernId.setValue(String(created.id));
        } else {
          this.jobPositions.update(items => [...items, created].sort((a, b) => a.name.localeCompare(b.name)));
          this.form.controls.appliedJobPositionId.setValue(String(created.id));
        }
      },
      error: error => this.error.set(this.service.errorMessage(error, 'Could not create the entry.'))
    });
  }

  protected onCvPicked(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;

    if (!file) {
      this.cvFile.set(null);
      return;
    }

    const maxMb = this.settings.display().maxUploadMb;

    if (!/\.(pdf|doc|docx)$/i.test(file.name)) {
      this.cvFile.set(null);
      input.value = '';
      this.error.set('The CV must be a PDF, DOC or DOCX file.');
      return;
    }

    if (file.size > maxMb * 1024 * 1024) {
      this.cvFile.set(null);
      input.value = '';
      this.error.set(`The CV is larger than the ${maxMb} MB upload limit.`);
      return;
    }

    this.error.set('');
    this.cvFile.set(file);
  }

  protected clearCv(): void {
    this.cvFile.set(null);
    const input = document.getElementById('cv-file') as HTMLInputElement | null;
    if (input) {
      input.value = '';
    }
  }

  protected save(): void {
    this.form.markAllAsTouched();

    if (this.form.invalid) {
      this.error.set('Please correct the highlighted fields.');
      return;
    }

    if (this.duplicates().length > 0 && !this.confirmDuplicate()) {
      this.error.set('A possible duplicate exists. Open the existing record, or tick "Save anyway" to continue.');
      return;
    }

    this.error.set('');
    this.saving.set(true);

    const request = this.toRequest();
    const id = this.candidateId();
    const call = id ? this.service.update(id, request) : this.service.create(request);

    call.subscribe({
      next: candidate => {
        const file = this.cvFile();

        if (!id && file) {
          this.service.uploadDocument(candidate.id, file, DOCUMENT_CATEGORIES.cv, null, false).subscribe({
            next: () => {
              this.saving.set(false);
              this.router.navigate(['/candidates', candidate.id]);
            },
            error: error => {
              this.saving.set(false);
              this.router.navigate(['/candidates', candidate.id], {
                state: { cvUploadError: this.service.errorMessage(error, 'The file was not accepted.') }
              });
            }
          });
          return;
        }

        this.saving.set(false);
        this.router.navigate(['/candidates', candidate.id]);
      },
      error: error => {
        this.saving.set(false);
        this.error.set(this.service.errorMessage(error, 'Could not save the candidate.'));

        if (error instanceof Object && 'status' in error && (error as { status?: number }).status === 409) {
          this.checkDuplicates();
        }
      }
    });
  }

  private toRequest(): CandidateSaveRequest {
    const value = this.form.getRawValue();

    return {
      name: value.name.trim(),
      mobileNumber: this.text(value.mobileNumber),
      email: this.text(value.email),
      dateOfBirth: this.date(value.dateOfBirth),
      presentAddress: this.text(value.presentAddress),
      currentOrganization: this.text(value.currentOrganization),
      currentDesignation: this.text(value.currentDesignation),
      durationInCurrentOrganization: this.text(value.durationInCurrentOrganization),
      totalYearsOfExperience: this.number(value.totalYearsOfExperience),
      appliedJobPositionId: this.number(value.appliedJobPositionId),
      concernId: this.number(value.concernId),
      presentSalary: this.number(value.presentSalary),
      expectedSalary: this.number(value.expectedSalary),
      noticePeriod: this.text(value.noticePeriod),
      remarks: this.text(value.remarks),
      status: Number(value.status),
      acceptPossibleDuplicate: this.confirmDuplicate()
    };
  }

  private patch(candidate: Candidate): void {
    this.form.patchValue({
      name: candidate.name,
      mobileNumber: candidate.mobileNumber ?? '',
      email: candidate.email ?? '',
      dateOfBirth: candidate.dateOfBirth?.slice(0, 10) ?? '',
      presentAddress: candidate.presentAddress ?? '',
      currentOrganization: candidate.currentOrganization ?? '',
      currentDesignation: candidate.currentDesignation ?? '',
      durationInCurrentOrganization: candidate.durationInCurrentOrganization ?? '',
      totalYearsOfExperience: this.asString(candidate.totalYearsOfExperience),
      appliedJobPositionId: this.asString(candidate.appliedJobPositionId),
      concernId: this.asString(candidate.concernId),
      presentSalary: this.asString(candidate.presentSalary),
      expectedSalary: this.asString(candidate.expectedSalary),
      noticePeriod: candidate.noticePeriod ?? '',
      remarks: candidate.remarks ?? '',
      status: String(candidate.status)
    });
  }

  private asString(value: number | null): string {
    return value === null || value === undefined ? '' : String(value);
  }

  private text(value: string): string | null {
    const trimmed = value?.trim();
    return trimmed ? trimmed : null;
  }

  private number(value: string | number | null): number | null {
    if (value === null || value === '' || value === undefined) {
      return null;
    }

    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : null;
  }

  private date(value: string): string | null {
    return value ? new Date(`${value}T00:00:00Z`).toISOString() : null;
  }

  protected invalid(controlName: string): boolean {
    const control = this.form.get(controlName);
    return !!control && control.invalid && (control.dirty || control.touched);
  }
}
