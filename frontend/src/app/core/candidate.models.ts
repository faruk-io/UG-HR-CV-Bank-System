export interface CandidateListItem {
  id: number;
  candidateCode: string;
  name: string;
  mobileNumber: string | null;
  email: string | null;
  currentOrganization: string | null;
  currentDesignation: string | null;
  appliedPosition: string | null;
  concern: string | null;
  totalYearsOfExperience: number | null;
  status: number;
  hasCv: boolean;
  createdAtUtc: string;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface CandidateDocument {
  id: number;
  category: number;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  isCurrent: boolean;
  uploadedAtUtc: string;
  uploadedByName: string | null;
  remarks: string | null;
}

export interface Candidate {
  id: number;
  candidateCode: string;
  name: string;
  mobileNumber: string | null;
  email: string | null;
  dateOfBirth: string | null;
  presentAddress: string | null;
  currentOrganization: string | null;
  currentDesignation: string | null;
  durationInCurrentOrganization: string | null;
  totalYearsOfExperience: number | null;
  appliedJobPositionId: number | null;
  appliedPosition: string | null;
  concernId: number | null;
  concern: string | null;
  presentSalary: number | null;
  expectedSalary: number | null;
  noticePeriod: string | null;
  remarks: string | null;
  status: number;
  createdAtUtc: string;
  createdByName: string | null;
  updatedAtUtc: string | null;
  interviewCount: number;
  documents: CandidateDocument[];
}

export interface CandidateSaveRequest {
  name: string;
  mobileNumber: string | null;
  email: string | null;
  dateOfBirth: string | null;
  presentAddress: string | null;
  currentOrganization: string | null;
  currentDesignation: string | null;
  durationInCurrentOrganization: string | null;
  totalYearsOfExperience: number | null;
  appliedJobPositionId: number | null;
  concernId: number | null;
  presentSalary: number | null;
  expectedSalary: number | null;
  noticePeriod: string | null;
  remarks: string | null;
  status: number | null;
  acceptPossibleDuplicate: boolean;
}

export interface DuplicateCandidate {
  id: number;
  candidateCode: string;
  name: string;
  mobileNumber: string | null;
  email: string | null;
  status: number;
  matchedOn: string;
}

export interface CandidateQuery {
  search?: string | null;
  status?: number | null;
  jobPositionId?: number | null;
  concernId?: number | null;
  minExperience?: number | null;
  maxExperience?: number | null;
  hasCv?: boolean | null;
  page?: number;
  pageSize?: number;
}

export interface MasterDataOption {
  id: number;
  name: string;
}

export const CANDIDATE_STATUSES = [
  { value: 1, label: 'Short-listed' },
  { value: 3, label: 'Optional (Back up)' },
  { value: 4, label: 'Over Budget' },
  { value: 2, label: 'Selected' },
  { value: 5, label: 'Fresher' }
] as const;

export const DOCUMENT_CATEGORIES = {
  cv: 1,
  completedEvaluationForm: 2,
  other: 3
} as const;

export function statusLabel(status: number): string {
  return CANDIDATE_STATUSES.find(s => s.value === status)?.label ?? 'Unknown';
}

export function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}
