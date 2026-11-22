import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { CandidateService } from '../../core/candidate.service';
import { MasterDataOption, PagedResult } from '../../core/candidate.models';
import { EvaluationService } from '../../core/evaluation.service';
import {
  INTERVIEW_DECISIONS,
  INTERVIEW_STATUSES,
  EvaluationListItem,
  EvaluationQuery,
  interviewDecisionLabel,
  interviewStatusLabel
} from '../../core/evaluation.models';

function blankQuery(): EvaluationQuery {
  return {
    search: null,
    candidateId: null,
    jobPositionId: null,
    status: null,
    decision: null,
    dateFrom: null,
    dateTo: null,
    hasScannedForm: null,
    page: 1,
    pageSize: 20
  };
}

import { Icon } from '../../layout/icon/icon';

@Component({
  selector: 'app-evaluations',
  imports: [CommonModule, FormsModule, RouterLink, Icon],
  templateUrl: './evaluations.html',
  styleUrl: './evaluations.scss'
})
export class Evaluations implements OnInit {
  protected readonly auth = inject(AuthService);
  private readonly service = inject(EvaluationService);
  private readonly master = inject(CandidateService);

  protected readonly query = signal<EvaluationQuery>(blankQuery());
  protected readonly page = signal<PagedResult<EvaluationListItem>>({ items: [], total: 0, page: 1, pageSize: 20 });
  protected readonly loading = signal(false);
  protected readonly error = signal('');
  protected readonly jobPositions = signal<MasterDataOption[]>([]);
  protected readonly statuses = INTERVIEW_STATUSES;
  protected readonly decisions = INTERVIEW_DECISIONS;

  private searchTimer: ReturnType<typeof setTimeout> | undefined;

  ngOnInit(): void {
    this.master.jobPositions().subscribe(items => this.jobPositions.set(items));
    this.load();
  }

  protected get q(): EvaluationQuery {
    return this.query();
  }

  protected get from(): number {
    const { total, page, pageSize } = this.page();
    return total === 0 ? 0 : (page - 1) * pageSize + 1;
  }

  protected get to(): number {
    const { total, page, pageSize } = this.page();
    return Math.min(page * pageSize, total);
  }

  protected get lastPage(): number {
    return Math.max(1, Math.ceil(this.page().total / this.page().pageSize));
  }

  protected patch(partial: Partial<EvaluationQuery>): void {
    this.query.update(q => ({ ...q, ...partial }));
    this.load();
  }

  protected onSearch(value: string): void {
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => this.patch({ search: value, page: 1 }), 350);
  }

  protected hasFilters(): boolean {
    const { search, candidateId, jobPositionId, status, decision, dateFrom, dateTo, hasScannedForm } = this.q;
    return !!(search || candidateId || jobPositionId || status || decision || dateFrom || dateTo) || hasScannedForm !== null;
  }

  protected clearFilters(): void {
    this.query.set(blankQuery());
    this.load();
  }

  protected statusLabel(status: number): string {
    return interviewStatusLabel(status);
  }

  protected decisionLabel(decision: number): string {
    return interviewDecisionLabel(decision);
  }

  private load(): void {
    this.loading.set(true);
    this.error.set('');

    this.service.list(this.query()).subscribe({
      next: result => {
        this.page.set(result);
        this.loading.set(false);
      },
      error: error => {
        this.error.set(this.service.errorMessage(error, 'Could not load the evaluation archive.'));
        this.loading.set(false);
      }
    });
  }
}
