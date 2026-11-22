import { CandidateDocument } from './candidate.models';

export interface EvaluationListItem {
  id: number;
  candidateId: number;
  candidateCode: string;
  candidateName: string;
  appliedPosition: string | null;
  concern: string | null;
  interviewNumber: number;
  interviewDate: string;
  status: number;
  decision: number;
  totalRating: number;
  maxRating: number;
  hasScannedForm: boolean;
  createdAtUtc: string;
  createdByName: string | null;
}

export interface EvaluationQuery {
  search?: string | null;
  candidateId?: number | null;
  jobPositionId?: number | null;
  status?: number | null;
  decision?: number | null;
  dateFrom?: string | null;
  dateTo?: string | null;
  hasScannedForm?: boolean | null;
  page?: number;
  pageSize?: number;
}

export interface EvaluationRatingLine {
  dimensionCode: number;
  dimensionName: string;
  rating: number;
}

export interface EvaluationDocumentItemLine {
  itemCode: number;
  itemName: string;
  details: string | null;
  copySubmitted: boolean;
  originalChecked: boolean;
}

export interface EvaluationReferenceLine {
  referenceNumber: number;
  referenceName: string | null;
  designation: string | null;
  relationship: string | null;
  companyAndLocation: string | null;
  phone: string | null;
  statement: string | null;
}

export interface EvaluationEvaluatorLine {
  sectionNumber: number;
  findings: string | null;
  evaluatorName: string | null;
  designation: string | null;
  department: string | null;
  signatureText: string | null;
}

export interface EvaluationApprovalLine {
  authorityNumber: number;
  authorityTitle: string;
  decision: string | null;
  signatureText: string | null;
  actedAtUtc: string | null;
}

export interface EvaluationResultLine {
  decision: number;
  recommendation: string | null;
  expectedSalary: number | null;
  noticePeriod: string | null;
  designationFixedId: number | null;
  designationFixed: string | null;
  salaryFixed: number | null;
  grade: string | null;
  dateOfJoining: string | null;
  remarks: string | null;
}

export interface EvaluationFormCandidate {
  candidateId: number;
  candidateCode: string;
  name: string;
  mobileNumber: string | null;
  email: string | null;
  dateOfBirth: string | null;
  currentOrganization: string | null;
  currentDesignation: string | null;
  presentSalary: number | null;
  durationInCurrentOrganization: string | null;
  totalYearsOfExperience: number | null;
  presentAddress: string | null;
  expectedSalary: number | null;
  noticePeriod: string | null;
}

export interface Evaluation {
  id: number;
  candidateId: number;
  candidateCode: string;
  interviewNumber: number;
  interviewDate: string;
  status: number;
  appliedJobPositionId: number | null;
  appliedPosition: string | null;
  concernId: number | null;
  concern: string | null;
  personsInterviewTaken: number | null;
  remarks: string | null;
  totalRating: number;
  maxRating: number;
  ratingScaleText: string;
  candidate: EvaluationFormCandidate;
  ratings: EvaluationRatingLine[];
  documentItems: EvaluationDocumentItemLine[];
  references: EvaluationReferenceLine[];
  evaluators: EvaluationEvaluatorLine[];
  result: EvaluationResultLine | null;
  approvals: EvaluationApprovalLine[];
  scannedForms: CandidateDocument[];
  createdAtUtc: string;
  createdById: number | null;
  createdByName: string | null;
}

export interface RatingInput {
  dimensionCode: number;
  rating: number;
}

export interface DocumentItemInput {
  itemCode: number;
  details: string | null;
  copySubmitted: boolean;
  originalChecked: boolean;
}

export interface ReferenceInput {
  referenceNumber: number;
  referenceName: string | null;
  designation: string | null;
  relationship: string | null;
  companyAndLocation: string | null;
  phone: string | null;
  statement: string | null;
}

export interface EvaluatorInput {
  sectionNumber: number;
  findings: string | null;
  evaluatorName: string | null;
  designation: string | null;
  department: string | null;
  signatureText: string | null;
}

export interface ApprovalInput {
  authorityNumber: number;
  decision: string | null;
  signatureText: string | null;
}

export interface ResultInput {
  decision: number;
  recommendation: string | null;
  expectedSalary: number | null;
  noticePeriod: string | null;
  designationFixedId: number | null;
  salaryFixed: number | null;
  grade: string | null;
  dateOfJoining: string | null;
  remarks: string | null;
}

export interface EvaluationSaveRequest {
  candidateId: number;
  interviewNumber: number | null;
  interviewDate: string;
  status: number;
  appliedJobPositionId: number | null;
  concernId: number | null;
  personsInterviewTaken: number | null;
  remarks: string | null;
  ratings: RatingInput[];
  documentItems: DocumentItemInput[];
  references: ReferenceInput[];
  evaluators: EvaluatorInput[];
  result: ResultInput | null;
  approvals: ApprovalInput[];
}

export interface CandidateEvaluationHistoryItem {
  id: number;
  interviewNumber: number;
  interviewDate: string;
  status: number;
  appliedPosition: string | null;
  concern: string | null;
  totalRating: number;
  maxRating: number;
  decision: number;
  recommendation: string | null;
  evaluatorNames: string | null;
  hasScannedForm: boolean;
  fixedDesignation: string | null;
  fixedSalary: number | null;
  grade: string | null;
  dateOfJoining: string | null;
  createdByName: string | null;
}

export const INTERVIEW_STATUSES = [
  { value: 1, label: 'Pending' },
  { value: 2, label: 'Completed' },
  { value: 3, label: 'Cancelled' }
] as const;

export const INTERVIEW_DECISIONS = [
  { value: 0, label: 'Not taken' },
  { value: 1, label: 'Selected' },
  { value: 2, label: 'Consider' },
  { value: 3, label: 'Reinterview' },
  { value: 4, label: 'Rejected' }
] as const;

export const RATING_CHOICES = [0, 1, 2, 3, 4, 5] as const;

export const APPROVAL_DECISIONS = ['Approved', 'Noted', 'Rejected'] as const;

export function interviewStatusLabel(status: number): string {
  return INTERVIEW_STATUSES.find(s => s.value === status)?.label ?? 'Unknown';
}

export function interviewDecisionLabel(decision: number): string {
  return INTERVIEW_DECISIONS.find(d => d.value === decision)?.label ?? 'Not taken';
}
