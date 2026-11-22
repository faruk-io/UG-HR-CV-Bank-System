import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { CandidateService } from '../../core/candidate.service';
import { Icon } from '../../layout/icon/icon';
import {
  CANDIDATE_STATUSES,
  CandidateListItem,
  CandidateQuery,
  MasterDataOption,
  PagedResult,
  statusLabel
} from '../../core/candidate.models';

@Component({
  selector: 'app-candidates',
  imports: [CommonModule, FormsModule, RouterLink, Icon],
  templateUrl: './candidates.html',
  styleUrl: './candidates.scss'
})
export class Candidates implements OnInit {
  protected readonly auth = inject(AuthService);
  private readonly service = inject(CandidateService);

  protected readonly query = signal<CandidateQuery>({ page: 1, pageSize: 20 });
  protected readonly page = signal<PagedResult<CandidateListItem>>({ items: [], total: 0, page: 1, pageSize: 20 });
  protected readonly loading = signal(false);
  protected readonly error = signal('');
  protected readonly deletingId = signal<number | null>(null);
  protected readonly jobPositions = signal<MasterDataOption[]>([]);
  protected readonly statuses = CANDIDATE_STATUSES;

  private searchTimer: ReturnType<typeof setTimeout> | undefined;

  ngOnInit(): void {
    this.service.jobPositions().subscribe(items => this.jobPositions.set(items));
    this.load();
  }

  protected get q(): CandidateQuery {
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

  protected patch(partial: Partial<CandidateQuery>): void {
    this.query.update(q => ({ ...q, ...partial }));
    this.load();
  }

  protected onSearch(value: string): void {
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => this.patch({ search: value, page: 1 }), 350);
  }

  protected clearFilters(): void {
    this.query.set({ page: 1, pageSize: this.q.pageSize });
    this.load();
  }

  protected hasFilters(): boolean {
    const { search, status, jobPositionId } = this.q;
    return !!(search || status || jobPositionId);
  }

  protected label(status: number): string {
    return statusLabel(status);
  }

  protected deleteCandidate(row: CandidateListItem): void {
    if (!confirm(`Delete candidate ${row.candidateCode} (${row.name}) permanently?`)) {
      return;
    }

    this.deletingId.set(row.id);
    this.service.remove(row.id).subscribe({
      next: () => {
        this.deletingId.set(null);
        this.load();
      },
      error: error => {
        this.deletingId.set(null);
        this.error.set(this.service.errorMessage(error, 'Could not delete the candidate.'));
      }
    });
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
        this.error.set(this.service.errorMessage(error, 'Could not load the candidate list.'));
        this.loading.set(false);
      }
    });
  }
}
