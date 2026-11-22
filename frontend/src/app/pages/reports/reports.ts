import { Component, DestroyRef, OnInit, computed, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CandidateService } from '../../core/candidate.service';
import { MasterDataOption } from '../../core/candidate.models';
import { AuthService } from '../../core/auth.service';
import { ReportPdfService } from '../../core/report-pdf.service';
import { ReportService } from '../../core/report.service';
import {
  CANDIDATE_STATUSES,
  CV_ON_FILE_OPTIONS,
  GROUP_BY_OPTIONS,
  INTERVIEW_VIEW_OPTIONS,
  ReportCell,
  ReportColumn,
  ReportData,
  ReportDefinition,
  ReportFilterKey,
  ReportQuery,
  blankReportQuery,
  cellClass,
  formatCell
} from '../../core/report.models';

import { Icon } from '../../layout/icon/icon';

const reportIcons: Record<string, string> = {
  'candidate-report': 'file-text',
  'group-by-candidates': 'chart',
  'interview-report': 'clipboard'
};

/** Free-text answers are the only columns allowed to break onto several lines. */
const WRAPPED_KEYS = new Set(['remarks', 'recommendation']);

@Component({
  selector: 'app-reports',
  imports: [CommonModule, FormsModule, Icon],
  templateUrl: './reports.html',
  styleUrl: './reports.scss'
})
export class Reports implements OnInit {
  protected readonly auth = inject(AuthService);
  private readonly service = inject(ReportService);
  private readonly pdf = inject(ReportPdfService);
  private readonly master = inject(CandidateService);

  protected readonly catalog = signal<ReportDefinition[]>([]);
  protected readonly active = signal<ReportDefinition | null>(null);
  protected readonly report = signal<ReportData | null>(null);
  protected readonly query = signal<ReportQuery>(blankReportQuery());
  protected readonly loading = signal(false);
  protected readonly exporting = signal(false);
  protected readonly error = signal('');
  protected readonly jobPositions = signal<MasterDataOption[]>([]);
  protected readonly concerns = signal<MasterDataOption[]>([]);
  protected readonly statuses = CANDIDATE_STATUSES;
  protected readonly cvOnFileOptions = CV_ON_FILE_OPTIONS;
  protected readonly groupByOptions = GROUP_BY_OPTIONS;
  protected readonly interviewViewOptions = INTERVIEW_VIEW_OPTIONS;

  private runSequence = 0;
  private searchTimer: ReturnType<typeof setTimeout> | undefined;
  protected readonly printedAt = signal('');

  constructor() {
    const root = document.documentElement;
    // The paper name must sit on <html>; on any inner box Chrome prints an empty first page.
    effect(() => root.classList.toggle('report-landscape', this.wide()));
    inject(DestroyRef).onDestroy(() => root.classList.remove('report-landscape'));
  }

  ngOnInit(): void {
    this.master.jobPositions().subscribe(items => this.jobPositions.set(items));
    this.master.concerns().subscribe(items => this.concerns.set(items));

    this.service.catalog().subscribe({
      next: items => {
        this.catalog.set(items);
        this.select(items[0]);
      },
      error: () => this.error.set('The report list could not be loaded.')
    });
  }

  protected get q(): ReportQuery {
    return this.query();
  }

  protected readonly rowCount = computed(() => this.report()?.rows.length ?? 0);

  /** Wide reports need a sideways sheet; the PDF export already does this, print must match. */
  protected wide(): boolean {
    return (this.report()?.columns.length ?? 0) > 8;
  }

  protected show(filter: ReportFilterKey): boolean {
    return this.active()?.filters.includes(filter) ?? false;
  }

  protected select(definition: ReportDefinition): void {
    this.active.set(definition);
    this.run();
  }

  protected patch(partial: Partial<ReportQuery>): void {
    this.query.update(current => ({ ...current, ...partial }));
    this.run();
  }

  protected clearFilters(): void {
    this.query.set(blankReportQuery());
    this.run();
  }

  protected onSearch(value: string | null): void {
    this.query.update(current => ({ ...current, search: value }));
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => this.run(), 350);
  }

  protected hasFilters(): boolean {
    const q = this.query();
    return !!(
      q.dateFrom ||
      q.dateTo ||
      q.search ||
      q.jobPositionId ||
      q.concernId ||
      q.status ||
      (q.cvOnFile && q.cvOnFile !== 'all')
    );
  }

  protected cell(column: ReportColumn, value: ReportCell): string {
    return formatCell(column, value);
  }

  protected align(column: ReportColumn): string {
    return cellClass(column);
  }

  protected classes(column: ReportColumn): string {
    const wrap = WRAPPED_KEYS.has(column.key) ? 'wrap' : '';
    return `${cellClass(column)} ${wrap}`.trim();
  }

  protected iconFor(code: string): string {
    return reportIcons[code] ?? 'table';
  }

  protected downloadExcel(): void {
    const definition = this.active();
    if (!definition) return;

    this.exporting.set(true);
    this.service.excel(definition.code, this.query()).subscribe({
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
    const data = this.report();
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

  protected print(): void {
    window.print();
  }

  private run(): void {
    const definition = this.active();
    if (!definition) return;

    const sequence = ++this.runSequence;
    this.loading.set(true);
    this.error.set('');
    this.report.set(null);

    this.service.run(definition.code, this.query()).subscribe({
      next: data => {
        if (sequence !== this.runSequence) return;
        this.report.set(data);
        this.printedAt.set(new Date().toLocaleString());
        this.loading.set(false);
      },
      error: error => {
        if (sequence !== this.runSequence) return;
        this.error.set(this.service.errorMessage(error, 'The report could not be produced.'));
        this.loading.set(false);
      }
    });
  }

  private fileName(extension: string): string {
    const definition = this.active();
    const slug = (definition?.code ?? 'report').replace(/-/g, '_');
    return `${slug}-${new Date().toISOString().slice(0, 10)}.${extension}`;
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
