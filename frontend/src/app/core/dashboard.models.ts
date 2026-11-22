export interface DashboardSummary {
  totalCandidates: number;
  totalCvUploaded: number;
  totalInterviews: number;
  selected: number;
  recentCandidates: RecentCandidate[];
  recentInterviews: RecentInterview[];
  interviewByDate: DateWiseInterview[];
  totalJoining: number;
  recentJoining: RecentJoining[];
  totalResigned: number;
  recentResigned: RecentResigned[];
}

export interface RecentCandidate {
  id: number;
  candidateCode: string;
  name: string;
  mobileNumber: string | null;
  appliedPosition: string | null;
  status: number;
  hasCv: boolean;
  createdAtUtc: string;
}

export interface RecentInterview {
  id: number;
  candidateId: number;
  candidateCode: string;
  candidateName: string;
  interviewNumber: number;
  interviewDate: string;
  status: number;
  decision: number;
}

export interface DateWiseInterview {
  date: string;
  total: number;
  selected: number;
}

export interface RecentJoining {
  id: number;
  employeeId: string;
  name: string;
  designation: string | null;
  dateOfJoining: string | null;
}

export interface RecentResigned {
  id: number;
  employeeId: string;
  name: string;
  resignDate: string | null;
}

export interface SummaryCandidateInfo {
  name: string;
  mobileNumber: string | null;
  email: string | null;
  dateOfBirth: string | null;
  presentAddress: string | null;
  remarks: string | null;
}

export interface SummaryJobInterview {
  appliedPosition: string | null;
  concern: string | null;
  interviewDate: string | null;
  interviewNumber: number | null;
  interviewStatus: string;
  finalDecision: string | null;
}

export interface SummaryEmployment {
  currentOrganization: string | null;
  currentDesignation: string | null;
  durationInCurrentOrganization: string | null;
  totalYearsOfExperience: number | null;
  presentSalary: number | null;
  expectedSalary: number | null;
  noticePeriod: string | null;
}

export interface RatingLine {
  dimension: string;
  rating: number;
  maxRating: number;
}

export interface SummaryEvaluation {
  interviewNumber: number;
  interviewDate: string;
  totalRating: number;
  maxRating: number;
  ratings: RatingLine[];
  evaluatorFindings: string | null;
  recommendation: string | null;
  decision: string;
  designationFixed: string | null;
  salaryFixed: number | null;
  grade: string | null;
  dateOfJoining: string | null;
}

export interface SummaryDocument {
  documentId: number;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  uploadedAtUtc: string;
}

export interface CandidateSummary {
  candidateId: number;
  candidateCode: string;
  generatedAtUtc: string;
  candidate: SummaryCandidateInfo;
  jobInterview: SummaryJobInterview;
  employment: SummaryEmployment;
  evaluation: SummaryEvaluation | null;
  cvDocuments: SummaryDocument[];
  otherDocuments: SummaryDocument[];
}

export const INTERVIEW_DECISIONS: Record<number, string> = {
  0: 'Not taken',
  1: 'Selected',
  2: 'Consider',
  3: 'Reinterview',
  4: 'Rejected'
};

export const INTERVIEW_STATUSES: Record<number, string> = {
  1: 'Pending',
  2: 'Completed',
  3: 'Cancelled'
};

export function decisionLabel(decision: number | null): string {
  if (decision === null || decision === undefined) return '-';
  return INTERVIEW_DECISIONS[decision] ?? 'Unknown';
}

export function interviewStatusLabel(status: number): string {
  return INTERVIEW_STATUSES[status] ?? 'Unknown';
}
