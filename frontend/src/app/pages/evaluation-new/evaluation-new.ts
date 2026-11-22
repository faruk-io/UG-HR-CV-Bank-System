import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CandidateService } from '../../core/candidate.service';
import { CandidateListItem, PagedResult } from '../../core/candidate.models';
import { EvaluationService } from '../../core/evaluation.service';
import { EvaluationSaveRequest, INTERVIEW_STATUSES } from '../../core/evaluation.models';
import { Icon } from '../../layout/icon/icon';

function today(): string {
  const now = new Date();
  const pad = (value: number) => String(value).padStart(2, '0');
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
}

@Component({
  selector: 'app-evaluation-new',
  imports: [CommonModule, FormsModule, RouterLink, Icon],
  templateUrl: './evaluation-new.html',
  styleUrl: './evaluation-new.scss'
})
export class EvaluationNew implements OnInit {
  private readonly candidates = inject(CandidateService);
  private readonly evaluations = inject(EvaluationService);
  private readonly router = inject(Router);

  protected readonly statuses = INTERVIEW_STATUSES;
  protected readonly results = signal<CandidateListItem[]>([]);
  protected readonly busy = signal(false);
  protected readonly error = signal('');
  protected readonly searched = signal(false);

  protected term = '';
  protected interviewDate = today();
  protected status = 1;

  private searchTimer: ReturnType<typeof setTimeout> | undefined;

  ngOnInit(): void {
    this.runSearch('');
  }

  protected onTerm(term: string): void {
    this.term = term;
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => this.runSearch(term.trim()), 350);
  }

  protected start(candidate: CandidateListItem): void {
    if (!this.interviewDate) {
      this.error.set('Select the interview date first.');
      return;
    }

    this.busy.set(true);
    this.error.set('');

    this.evaluations.create(this.newRequest(candidate.id)).subscribe({
      next: evaluation => {
        this.busy.set(false);
        this.router.navigate(['/evaluations', evaluation.id]);
      },
      error: error => {
        this.busy.set(false);
        this.error.set(this.evaluations.errorMessage(error, 'Could not start the evaluation.'));
      }
    });
  }

  private newRequest(candidateId: number): EvaluationSaveRequest {
    return {
      candidateId,
      interviewNumber: null,
      interviewDate: `${this.interviewDate}T00:00:00Z`,
      status: this.status,
      appliedJobPositionId: null,
      concernId: null,
      personsInterviewTaken: null,
      remarks: null,
      ratings: [],
      documentItems: [],
      references: [],
      evaluators: [],
      result: null,
      approvals: []
    };
  }

  private runSearch(term: string): void {
    this.busy.set(true);
    this.candidates.list({ search: term || null, page: 1, pageSize: 25 }).subscribe({
      next: (result: PagedResult<CandidateListItem>) => {
        this.results.set(result.items);
        this.searched.set(true);
        this.busy.set(false);
      },
      error: error => {
        this.error.set(this.candidates.errorMessage(error, 'Could not load candidates.'));
        this.busy.set(false);
      }
    });
  }
}
