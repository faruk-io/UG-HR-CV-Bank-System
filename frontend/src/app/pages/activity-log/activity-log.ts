import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  ActivityLogOptions,
  ActivityLogQuery,
  dayRange,
  defaultActivityLogQuery
} from '../../core/activity-log.models';
import { ActivityLogService } from '../../core/activity-log.service';
import { ReportPdfService } from '../../core/report-pdf.service';
import { ReportCell, ReportColumn, ReportData, cellClass, formatCell } from '../../core/report.models';

import { Icon } from '../../layout/icon/icon';

@Component({
  selector: 'app-activity-log',
  imports: [CommonModule, FormsModule, Icon],
  templateUrl: './activity-log.html',
  styleUrl: './activity-log.scss'
})
export class ActivityLog implements OnInit {
  private readonly service = inject(ActivityLogService);
  private readonly pdf = inject(ReportPdfService);

  protected readonly pageSizes = [25, 50, 100];

  protected readonly options = signal<ActivityLogOptions>({ actors: [], actions: [], entityTypes: [] });
  protected readonly log = signal<ReportData | null>(null);
  protected readonly query = signal<ActivityLogQuery>(defaultActivityLogQuery());
  protected readonly loading = signal(false);
  protected readonly exporting = signal(false);
  protected readonly error = signal('');
  protected readonly pageSize = signal(50);
  protected readonly page = signal(0);

  private runSequence = 0;
  private searchTimer: ReturnType<typeof setTimeout> | undefined;

  ngOnInit(): void {
    this.loadOptions();
    this.run();
  }

  protected get q(): ActivityLogQuery {
    return this.query();
  }

  protected readonly rowCount = computed(() => this.log()?.rows.length ?? 0);
  protected readonly pageCount = computed(() => Math.max(1, Math.ceil(this.rowCount() / this.pageSize())));
  protected readonly pageNumber = computed(() => this.page() + 1);
  protected readonly firstRow = computed(() => (this.rowCount() === 0 ? 0 : this.page() * this.pageSize() + 1));
  protected readonly lastRow = computed(() => Math.min((this.page() + 1) * this.pageSize(), this.rowCount()));
  protected readonly rangeLabel = computed(() =>
    this.rowCount() === 0 ? 'no rows to show' : `showing ${this.firstRow()}-${this.lastRow()}`);
  protected readonly pageRows = computed(() =>
    (this.log()?.rows ?? []).slice(this.page() * this.pageSize(), (this.page() + 1) * this.pageSize()));

  protected patch(partial: Partial<ActivityLogQuery>): void {
    this.query.update(current => ({ ...current, ...partial }));
    this.run();
  }

  protected onSearch(value: string | null): void {
    this.query.update(current => ({ ...current, search: value }));
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => this.run(), 350);
  }

  protected preset(daysBack: number): void {
    this.query.update(current => ({ ...current, ...dayRange(daysBack) }));
    this.run();
  }

  protected isPreset(daysBack: number): boolean {
    const range = dayRange(daysBack);
    const q = this.query();
    return q.dateFrom === range.dateFrom && q.dateTo === range.dateTo;
  }

  protected isAllTime(): boolean {
    const q = this.query();
    return !q.dateFrom && !q.dateTo;
  }

  protected showAllTime(): void {
    this.query.update(current => ({ ...current, dateFrom: null, dateTo: null }));
    this.run();
  }

  protected clearFilters(): void {
    this.query.set(defaultActivityLogQuery());
    this.run();
  }

  protected hasFilters(): boolean {
    const q = this.query();
    const standard = defaultActivityLogQuery();
    return (
      !!(q.actor || q.action || q.entityType || q.search) ||
      q.dateFrom !== standard.dateFrom ||
      q.dateTo !== standard.dateTo
    );
  }

  protected setPageSize(size: number): void {
    this.pageSize.set(size);
    this.page.set(0);
  }

  protected stepPage(delta: number): void {
    this.page.update(current => Math.min(Math.max(current + delta, 0), this.pageCount() - 1));
  }

  protected cell(column: ReportColumn, value: ReportCell): string {
    return formatCell(column, value);
  }

  protected align(column: ReportColumn): string {
    return cellClass(column);
  }

  protected print(): void {
    window.print();
  }

  protected downloadExcel(): void {
    this.exporting.set(true);
    this.error.set('');

    this.service.excel(this.query()).subscribe({
      next: blob => {
        this.saveBlob(blob, this.fileName('xlsx'));
        this.exporting.set(false);
      },
      error: () => {
        this.error.set('The Excel file could not be produced.');
        this.exporting.set(false);
      }
    });
  }

  async downloadPdf(): Promise<void> {
    const data = this.log();
    if (!data || this.loading()) return;

    this.exporting.set(true);
    this.error.set('');

    try {
      await this.pdf.download(data, this.fileName('pdf'));
    } catch {
      this.error.set('The PDF file could not be produced.');
    } finally {
      this.exporting.set(false);
    }
  }

  private loadOptions(): void {
    this.service.options().subscribe(options => this.options.set(options));
  }

  private run(): void {
    const sequence = ++this.runSequence;
    this.loading.set(true);
    this.error.set('');
    this.page.set(0);

    this.service.run(this.query()).subscribe({
      next: data => {
        if (sequence !== this.runSequence) return;
        this.log.set(data);
        this.loading.set(false);
      },
      error: error => {
        if (sequence !== this.runSequence) return;
        this.error.set(this.service.errorMessage(error, 'The activity log could not be loaded.'));
        this.loading.set(false);
      }
    });
  }

  private fileName(extension: string): string {
    const stamp = new Date().toISOString().slice(0, 10);
    return `activity_log-${stamp}.${extension}`;
  }

  private saveBlob(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.click();
    URL.revokeObjectURL(url);
  }
}
