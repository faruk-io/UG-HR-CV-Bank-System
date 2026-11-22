export type ReportFilterKey =
  | 'dateRange'
  | 'search'
  | 'jobPosition'
  | 'concern'
  | 'status'
  | 'cvOnFile'
  | 'groupBy'
  | 'interviewView';

export type ReportColumnType = 'text' | 'number' | 'date' | 'datetime' | 'money' | 'decimal';

export interface ReportDefinition {
  code: string;
  title: string;
  description: string;
  filters: ReportFilterKey[];
  inMenu: boolean;
}

export interface ReportColumn {
  key: string;
  label: string;
  type: ReportColumnType;
}

export type ReportCell = string | number | null;

export interface ReportData {
  code: string;
  title: string;
  description: string;
  filterSummary: string;
  columns: ReportColumn[];
  rows: ReportCell[][];
  truncated: boolean;
}

export interface ReportQuery {
  dateFrom: string | null;
  dateTo: string | null;
  search: string | null;
  jobPositionId: number | null;
  concernId: number | null;
  status: number | null;
  cvOnFile: string;
  groupBy: string;
  interviewView: string;
}

export const CV_ON_FILE_OPTIONS = [
  { value: 'all', label: 'All' },
  { value: 'yes', label: 'With CV on file' },
  { value: 'no', label: 'Without CV' }
];

export const GROUP_BY_OPTIONS = [
  { value: 'organization', label: 'Organization' },
  { value: 'designation', label: 'Designation' },
  { value: 'position', label: 'Applied position' },
  { value: 'concern', label: 'Concern' },
  { value: 'status', label: 'Candidate status' },
  { value: 'registered-date', label: 'Registration date' }
];

export const INTERVIEW_VIEW_OPTIONS = [
  { value: 'detail', label: 'Every interview' },
  { value: 'date', label: 'By interview date' }
];

export const CANDIDATE_STATUSES = [
  { value: 1, label: 'Short-listed' },
  { value: 3, label: 'Optional (Back up)' },
  { value: 4, label: 'Over Budget' },
  { value: 2, label: 'Selected' },
  { value: 5, label: 'Fresher' }
];

export function blankReportQuery(): ReportQuery {
  return {
    dateFrom: null,
    dateTo: null,
    search: null,
    jobPositionId: null,
    concernId: null,
    status: null,
    cvOnFile: 'all',
    groupBy: 'organization',
    interviewView: 'detail'
  };
}

/** Renders one report cell for the table; 'date' arrives as yyyy-MM-dd and 'datetime' as server-local ISO. */
export function formatCell(column: ReportColumn, value: ReportCell): string {
  if (value === null || value === undefined) return '—';

  if (typeof value === 'number') {
    if (column.type === 'money') return value.toLocaleString('en-US');
    if (column.type === 'decimal') return value % 1 === 0 ? String(value) : value.toFixed(1);
    return String(value);
  }

  const parsed = new Date(column.type === 'date' ? `${value}T00:00:00` : value);
  if (column.type === 'date' || column.type === 'datetime') {
    return Number.isNaN(parsed.getTime())
      ? value
      : parsed.toLocaleString('en-GB', {
          day: '2-digit',
          month: 'short',
          year: 'numeric',
          ...(column.type === 'datetime' ? { hour: '2-digit', minute: '2-digit', second: '2-digit' } : {})
        });
  }

  return value;
}

export function cellClass(column: ReportColumn): string {
  return column.type === 'number' || column.type === 'money' || column.type === 'decimal'
    ? 'num'
    : column.type === 'date' || column.type === 'datetime'
      ? 'day'
      : '';
}
