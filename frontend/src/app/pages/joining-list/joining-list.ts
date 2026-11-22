import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { EmployeeListService } from '../../core/employee-list.service';
import { ReportPdfService } from '../../core/report-pdf.service';
import { EmployeeListQuery, JoiningRecord, JoiningSaveRequest, formatAmount, formatAmountInput, parseAmount } from '../../core/employee-list.models';
import { MasterDataOption, PagedResult } from '../../core/candidate.models';
import { Icon } from '../../layout/icon/icon';

interface JoiningDraft {
  employeeId: string;
  name: string;
  designation: string;
  department: string;
  salary: string;
  concernId: number | null;
  dateOfJoining: string;
  probationPeriod: string;
  afterProbationSalary: string;
  effectiveSalary: string;
  // effectiveSalary: number | null;
  remarks: string;
}

function emptyDraft(): JoiningDraft {
  return {
    employeeId: '',
    name: '',
    designation: '',
    department: '',
    salary: '',
    concernId: null,
    dateOfJoining: '',
    probationPeriod: '',
    afterProbationSalary: '',
    // effectiveSalary: null,
    effectiveSalary: '',
    remarks: ''
  };
}

@Component({
  selector: 'app-joining-list',
  imports: [CommonModule, FormsModule, Icon],
  templateUrl: './joining-list.html',
  styleUrl: './joining-list.scss'
})
export class JoiningList implements OnInit {
  private readonly service = inject(EmployeeListService);
  private readonly pdf = inject(ReportPdfService);

  protected readonly query = signal<EmployeeListQuery>({ page: 1, pageSize: 20 });
  protected readonly page = signal<PagedResult<JoiningRecord>>({ items: [], total: 0, page: 1, pageSize: 20 });
  protected readonly loading = signal(false);
  protected readonly exporting = signal(false);
  protected readonly error = signal('');
  protected readonly notice = signal('');
  protected readonly concerns = signal<MasterDataOption[]>([]);
  protected readonly deletingId = signal<number | null>(null);

  protected readonly editorOpen = signal(false);
  protected readonly editingId = signal<number | null>(null);
  protected readonly draft = signal<JoiningDraft>(emptyDraft());
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

    this.service.exportJoining(this.query()).subscribe({
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

    this.service.excelJoining(this.query()).subscribe({
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
    return `joining-list-${new Date().toISOString().slice(0, 10)}.${extension}`;
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

  protected openEdit(row: JoiningRecord): void {
    this.editingId.set(row.id);
    this.draft.set({
      employeeId: row.employeeId,
      name: row.name,
      designation: row.designation ?? '',
      department: row.department ?? '',
      salary: formatAmount(row.salary),
      concernId: row.concernId,
      dateOfJoining: row.dateOfJoining?.slice(0, 10) ?? '',
      probationPeriod: row.probationPeriod ?? '',
      afterProbationSalary: formatAmount(row.afterProbationSalary),
      // effectiveSalary: row.effectiveSalary,
      effectiveSalary: row.effectiveSalary ?? '',
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

  protected patchDraft(partial: Partial<JoiningDraft>): void {
    this.draft.update(current => ({ ...current, ...partial }));
  }

  /** Salary inputs: plain text field that groups thousands while typing (50,000). */
  protected onAmount(field: 'salary' | 'afterProbationSalary', raw: string): void {
    this.patchDraft({ [field]: formatAmountInput(raw ?? '') });
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

    /* if (!value.employeeId.trim() || !value.name.trim()) {
      this.formError.set('Employee ID and Name are required.');
      return;
    } */
   if (!value.name.trim()) {
  this.formError.set('Name is required.');
  return;
   }

    const request: JoiningSaveRequest = {
      employeeId: value.employeeId.trim(),
      name: value.name.trim(),
      designation: this.text(value.designation),
      department: this.text(value.department),
      salary: parseAmount(value.salary),
      concernId: value.concernId,
      dateOfJoining: value.dateOfJoining || null,
      probationPeriod: this.text(value.probationPeriod),
      afterProbationSalary: parseAmount(value.afterProbationSalary),
      // effectiveSalary: value.effectiveSalary,
      effectiveSalary: this.text(value.effectiveSalary),
      remarks: this.text(value.remarks)
    };

    this.saving.set(true);
    this.formError.set('');

    const id = this.editingId();
    const call = id ? this.service.updateJoining(id, request) : this.service.createJoining(request);

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

  protected deleteRow(row: JoiningRecord): void {
    if (!confirm(`Delete joining entry for ${row.employeeId} (${row.name}) permanently?`)) {
      return;
    }

    this.deletingId.set(row.id);
    this.service.removeJoining(row.id).subscribe({
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

    this.service.listJoining(this.query()).subscribe({
      next: result => {
        this.page.set(result);
        this.loading.set(false);
      },
      error: error => {
        this.error.set(this.service.errorMessage(error, 'Could not load the joining list.'));
        this.loading.set(false);
      }
    });
  }
}