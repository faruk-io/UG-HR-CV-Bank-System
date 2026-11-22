export interface JoiningRecord {
  id: number;
  employeeId: string;
  name: string;
  designation: string | null;
  department: string | null;
  salary: number | null;
  concernId: number | null;
  concern: string | null;
  dateOfJoining: string | null;
  probationPeriod: string | null;
  afterProbationSalary: number | null;
  effectiveSalary: string | null;
  // effectiveSalary: number | null;
  remarks: string | null;
  createdAtUtc: string;
  createdByName: string | null;
  updatedAtUtc: string | null;
}

export interface JoiningSaveRequest {
  employeeId: string;
  name: string;
  designation: string | null;
  department: string | null;
  salary: number | null;
  concernId: number | null;
  dateOfJoining: string | null;
  probationPeriod: string | null;
  afterProbationSalary: number | null;
  effectiveSalary: string | null; 
  // effectiveSalary: number | null;
  remarks: string | null;
}

export interface ResignedRecord {
  id: number;
  employeeId: string;
  name: string;
  designation: string | null;
  department: string | null;
  salary: number | null;
  concernId: number | null;
  concern: string | null;
  dateOfJoining: string | null;
  resignDate: string | null;
  lastWorkingDay: string | null;
  effectiveFrom: string | null;
  noticePeriod: string | null;
  hrApproved: boolean;
  causeOfResignation: string | null;
  remarks: string | null;
  createdAtUtc: string;
  createdByName: string | null;
  updatedAtUtc: string | null;
}

export interface ResignedSaveRequest {
  employeeId: string;
  name: string;
  designation: string | null;
  department: string | null;
  salary: number | null;
  concernId: number | null;
  dateOfJoining: string | null;
  resignDate: string | null;
  lastWorkingDay: string | null;
  effectiveFrom: string | null;
  noticePeriod: string | null;
  hrApproved: boolean;
  causeOfResignation: string | null;
  remarks: string | null;
}

export interface EmployeeListQuery {
  search?: string | null;
  concernId?: number | null;
  dateFrom?: string | null;
  dateTo?: string | null;
  page?: number;
  pageSize?: number;
}

/** Stored number -> grouped display text (50,000), matching the list's number pipe. */
export function formatAmount(value: number | null): string {
  return value === null ? '' : value.toLocaleString('en-US', { maximumFractionDigits: 2 });
}

/** Live input mask: group thousands while typing, keep a decimal part the user is entering. */
export function formatAmountInput(raw: string): string {
  const noCommas = raw.replace(/,/g, '');
  const dot = noCommas.indexOf('.');
  const intPart = (dot === -1 ? noCommas : noCommas.slice(0, dot)).replace(/\D/g, '');
  const grouped = intPart.replace(/\B(?=(\d{3})+(?!\d))/g, ',');
  if (dot === -1) {
    return grouped;
  }
  const decPart = noCommas.slice(dot + 1).replace(/\D/g, '').slice(0, 2);
  return `${grouped}.${decPart}`;
}

/** Grouped display text -> number for the API (null when empty). */
export function parseAmount(text: string): number | null {
  const cleaned = text.replace(/,/g, '').trim();
  if (!cleaned) {
    return null;
  }
  const value = Number(cleaned);
  return Number.isFinite(value) ? value : null;
}