import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { EmployeeListService } from '../../core/employee-list.service';
import { ReportPdfService } from '../../core/report-pdf.service';
import { EmployeeListQuery, ResignedRecord, ResignedSaveRequest, formatAmount, formatAmountInput, parseAmount } from '../../core/employee-list.models';
import { MasterDataOption, PagedResult } from '../../core/candidate.models';
import { Icon } from '../../layout/icon/icon';

interface ResignedDraft {
  employeeId: string;
  name: string;
  designation: string;
  department: string;
  salary: string;
  concernId: number | null;
  dateOfJoining: string;
  resignDate: string;
  lastWorkingDay: string;
  effectiveFrom: string;
  noticePeriod: string;
  hrApproved: boolean;
  causeOfResignation: string;
  remarks: string;
}

function emptyDraft(): ResignedDraft {
  return {
    employeeId: '',
    name: '',
    designation: '',
    department: '',
    salary: '',
    concernId: null,
    dateOfJoining: '',
    resignDate: '',
    lastWorkingDay: '',
    effectiveFrom: '',
    noticePeriod: '',
    hrApproved: false,
    causeOfResignation: '',
    remarks: ''
  };
}

@Component({
  selector: 'app-resigned-list',
  imports: [CommonModule, FormsModule, Icon],
  templateUrl: './resigned-list.html',
  styleUrl: './resigned-list.scss'
})
export class ResignedList implements OnInit {
  private readonly service = inject(EmployeeListService);
  private readonly pdf = inject(ReportPdfService);

  protected readonly query = signal<EmployeeListQuery>({ page: 1, pageSize: 20 });
  protected readonly page = signal<PagedResult<ResignedRecord>>({ items: [], total: 0, page: 1, pageSize: 20 });
  protected readonly loading = signal(false);
  protected readonly exporting = signal(false);
  protected readonly error = signal('');
  protected readonly notice = signal('');
  protected readonly concerns = signal<MasterDataOption[]>([]);
  protected readonly deletingId = signal<number | null>(null);

  protected readonly editorOpen = signal(false);
  protected readonly editingId = signal<number | null>(null);
  protected readonly draft = signal<ResignedDraft>(emptyDraft());
  protected readonly saving = signal(false);
  protected readonly formError = signal('');

  private searchTimer: ReturnType<typeof setTimeout> | undefined;

  ngOnInit(): void {
    this.service.concerns().subscribe(items => this.concerns.set(items));
    this.load();
  }

  protected get q(): EmployeeListQuery {
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

  protected sl(index: number): number {
    const { page, pageSize } = this.page();
    return (page - 1) * pageSize + index + 1;
  }

  protected patch(partial: Partial<EmployeeListQuery>): void {
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
    return !!(this.q.search || this.q.concernId || this.q.dateFrom || this.q.dateTo);
  }

  protected async downloadPdf(): Promise<void> {
    if (this.exporting()) return;

    this.exporting.set(true);
    this.error.set('');

    this.service.exportResigned(this.query()).subscribe({
      next: async data => {
        try {
          await this.pdf.download(data, this.fileName('pdf'));
        } catch {
          this.error.set('The PDF file could not be produced.');
        } finally {
          this.exporting.set(false);
        }
      },
      error: error => {
        this.error.set(this.service.errorMessage(error, 'The PDF export could not be loaded.'));
        this.exporting.set(false);
      }
    });
  }

  protected downloadExcel(): void {
    if (this.exporting()) return;

    this.exporting.set(true);
    this.error.set('');

    this.service.excelResigned(this.query()).subscribe({
      next: blob => {
        this.saveBlob(blob, this.fileName('xlsx'));
        this.exporting.set(false);
      },
      error: error => {
        this.error.set(this.service.errorMessage(error, 'The Excel file could not be produced.'));
        this.exporting.set(false);
      }
    });
  }

  private fileName(extension: string): string {
    return `resigned-list-${new Date().toISOString().slice(0, 10)}.${extension}`;
  }

  private saveBlob(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.click();
    URL.revokeObjectURL(url);
  }

  protected openNew(): void {
    this.editingId.set(null);
    this.draft.set(emptyDraft());
    this.formError.set('');
    this.editorOpen.set(true);
  }

  protected openEdit(row: ResignedRecord): void {
    this.editingId.set(row.id);
    this.draft.set({
      employeeId: row.employeeId,
      name: row.name,
      designation: row.designation ?? '',
      department: row.department ?? '',
      salary: formatAmount(row.salary),
      concernId: row.concernId,
      dateOfJoining: row.dateOfJoining?.slice(0, 10) ?? '',
      resignDate: row.resignDate?.slice(0, 10) ?? '',
      lastWorkingDay: row.lastWorkingDay?.slice(0, 10) ?? '',
      effectiveFrom: row.effectiveFrom?.slice(0, 10) ?? '',
      noticePeriod: row.noticePeriod ?? '',
      hrApproved: row.hrApproved,
      causeOfResignation: row.causeOfResignation ?? '',
      remarks: row.remarks ?? ''
    });
    this.formError.set('');
    this.editorOpen.set(true);
  }

  protected closeEditor(): void {
    if (!this.saving()) {
      this.editorOpen.set(false);
    }
  }

  protected patchDraft(partial: Partial<ResignedDraft>): void {
    this.draft.update(current => ({ ...current, ...partial }));
  }

  /** Salary input: plain text field that groups thousands while typing (50,000). */
  protected onAmount(raw: string): void {
    this.patchDraft({ salary: formatAmountInput(raw ?? '') });
  }

  protected addConcern(): void {
    const name = prompt('New concern name')?.trim();

    if (!name) {
      return;
    }

    this.service.addConcern(name).subscribe({
      next: created => {
        this.concerns.update(items => [...items, created].sort((a, b) => a.name.localeCompare(b.name)));
        this.patchDraft({ concernId: created.id });
      },
      error: error => this.formError.set(this.service.errorMessage(error, 'Could not create the concern.'))
    });
  }

  protected save(): void {
    const value = this.draft();

    if (!value.employeeId.trim() || !value.name.trim()) {
      this.formError.set('Employee ID and Name are required.');
      return;
    }

    const request: ResignedSaveRequest = {
      employeeId: value.employeeId.trim(),
      name: value.name.trim(),
      designation: this.text(value.designation),
      department: this.text(value.department),
      salary: parseAmount(value.salary),
      concernId: value.concernId,
      dateOfJoining: value.dateOfJoining || null,
      resignDate: value.resignDate || null,
      lastWorkingDay: value.lastWorkingDay || null,
      effectiveFrom: value.effectiveFrom || null,
      noticePeriod: this.text(value.noticePeriod),
      hrApproved: value.hrApproved,
      causeOfResignation: this.text(value.causeOfResignation),
      remarks: this.text(value.remarks)
    };

    this.saving.set(true);
    this.formError.set('');

    const id = this.editingId();
    const call = id ? this.service.updateResigned(id, request) : this.service.createResigned(request);

    call.subscribe({
      next: () => {
        this.saving.set(false);
        this.editorOpen.set(false);
        this.notice.set(id ? 'Entry updated.' : 'Entry saved.');
        this.load();
      },
      error: error => {
        this.saving.set(false);
        this.formError.set(this.service.errorMessage(error, 'Could not save the entry.'));
      }
    });
  }

  protected deleteRow(row: ResignedRecord): void {
    if (!confirm(`Delete resigned entry for ${row.employeeId} (${row.name}) permanently?`)) {
      return;
    }

    this.deletingId.set(row.id);
    this.service.removeResigned(row.id).subscribe({
      next: () => {
        this.deletingId.set(null);
        this.notice.set('Entry deleted.');
        this.load();
      },
      error: error => {
        this.deletingId.set(null);
        this.error.set(this.service.errorMessage(error, 'Could not delete the entry.'));
      }
    });
  }

  private text(value: string): string | null {
    const trimmed = value.trim();
    return trimmed.length === 0 ? null : trimmed;
  }

  private load(): void {
    this.loading.set(true);
    this.error.set('');

    this.service.listResigned(this.query()).subscribe({
      next: result => {
        this.page.set(result);
        this.loading.set(false);
      },
      error: error => {
        this.error.set(this.service.errorMessage(error, 'Could not load the resigned list.'));
        this.loading.set(false);
      }
    });
  }
}