import { Evaluation } from './evaluation.models';

export interface ChecklistRow {
  sl: number;
  name: string;
  details: string;
  copy: boolean;
  original: boolean;
}

export interface RatingRow {
  name: string;
  rating: string;
}

export interface ReferenceBlock {
  name: string;
  designation: string;
  relationship: string;
  company: string;
  phone: string;
  statement: string;
}

export interface EvaluatorBlock {
  findings: string;
  sign: string;
  name: string;
  designation: string;
  department: string;
}

export interface DecisionBox {
  label: string;
  marked: boolean;
}

export interface ApprovalBlock {
  title: string;
  decision: string;
  sign: string;
}

export interface InterviewFormModel {
  title: string;
  dateLabel: string;
  name: string;
  positionApplied: string;
  concernName: string;
  companyName: string;
  presentSalary: string;
  designation: string;
  duration: string;
  experience: string;
  dateOfBirth: string;
  presentAddress: string;
  documentsLeft: ChecklistRow[];
  documentsRight: ChecklistRow[];
  references: ReferenceBlock[];
  ratingScale: string;
  ratingsLeft: RatingRow[];
  ratingsRight: RatingRow[];
  totalRating: string;
  maxRating: number;
  evaluators: EvaluatorBlock[];
  personsTaken: string;
  expectedSalary: string;
  recommendation: string;
  noticePeriod: string;
  decisions: DecisionBox[];
  designationFixed: string;
  salaryFixed: string;
  grade: string;
  doj: string;
  remarks: string;
  approvals: ApprovalBlock[];
  footer: string;
}

const EMPTY = '—';

function text(value: string | number | null | undefined): string {
  const trimmed = `${value ?? ''}`.trim();
  return trimmed === '' ? EMPTY : trimmed;
}

function dateLabel(iso: string | null): string {
  if (!iso) return 'Date: ____ / ____ / ________';
  const d = new Date(iso);
  const pad = (n: number) => String(n).padStart(2, '0');
  return `Date: ${pad(d.getDate())} / ${pad(d.getMonth() + 1)} / ${d.getFullYear()}`;
}

export function buildInterviewFormModel(ev: Evaluation): InterviewFormModel {
  const candidate = ev.candidate;
  const result = ev.result;
  const items = ev.documentItems ?? [];
  const ratings = ev.ratings ?? [];

  const half = Math.ceil(items.length / 2);
  const toRow = (item: (typeof items)[number]): ChecklistRow => ({
    sl: item.itemCode,
    name: item.itemName,
    details: item.details ?? '',
    copy: item.copySubmitted,
    original: item.originalChecked
  });

  const ratingRow = (code: number): RatingRow => {
    const found = ratings.find(r => r.dimensionCode === code);
    return { name: found?.dimensionName ?? '', rating: found && found.rating > 0 ? String(found.rating) : '' };
  };

  const decision = result?.decision ?? 0;

  const blocks: EvaluatorBlock[] = (ev.evaluators ?? []).map(e => ({
    findings: e.findings ?? '',
    sign: e.signatureText ?? '',
    name: e.evaluatorName ?? '',
    designation: e.designation ?? '',
    department: e.department ?? ''
  }));
  // The official sheet seats two evaluators; extra panels only print when someone actually filled them.
  const filled = (block: EvaluatorBlock) => Object.values(block).some(value => value.trim() !== '');
  const evaluators = blocks.slice(2).some(filled) ? blocks : blocks.slice(0, 2);

  return {
    title: 'INTERVIEW EVALUATION FORM',
    dateLabel: dateLabel(ev.interviewDate),
    name: text(candidate.name),
    positionApplied: text(ev.appliedPosition),
    concernName: text(ev.concern),
    companyName: text(candidate.currentOrganization),
    presentSalary: text(candidate.presentSalary),
    designation: text(candidate.currentDesignation),
    duration: text(candidate.durationInCurrentOrganization),
    experience: text(candidate.totalYearsOfExperience),
    dateOfBirth: candidate.dateOfBirth ? new Date(candidate.dateOfBirth).toLocaleDateString('en-GB') : EMPTY,
    presentAddress: text(candidate.presentAddress),
    documentsLeft: items.slice(0, half).map(toRow),
    documentsRight: items.slice(half).map(toRow),
    references: (ev.references ?? []).map(ref => ({
      name: ref.referenceName ?? '',
      designation: ref.designation ?? '',
      relationship: ref.relationship ?? '',
      company: ref.companyAndLocation ?? '',
      phone: ref.phone ?? '',
      statement: ref.statement ?? ''
    })),
    ratingScale: ev.ratingScaleText,
    ratingsLeft: [1, 2, 3, 4, 5].map(ratingRow),
    ratingsRight: [6, 7, 8, 9, 10].map(ratingRow),
    totalRating: String(ev.totalRating ?? 0),
    maxRating: ev.maxRating ?? 50,
    evaluators,
    personsTaken: text(ev.personsInterviewTaken),
    expectedSalary: text(result?.expectedSalary ?? candidate.expectedSalary),
    recommendation: text(result?.recommendation),
    noticePeriod: text(result?.noticePeriod ?? candidate.noticePeriod),
    decisions: [
      { label: 'Selected', marked: decision === 1 },
      { label: 'Consider', marked: decision === 2 },
      { label: 'Reinterview', marked: decision === 3 },
      { label: 'Reject', marked: decision === 4 }
    ],
    designationFixed: text(result?.designationFixed),
    salaryFixed: text(result?.salaryFixed),
    grade: text(result?.grade),
    doj: result?.dateOfJoining ? new Date(result.dateOfJoining).toLocaleDateString('en-GB') : EMPTY,
    remarks: text(result?.remarks ?? ev.remarks),
    approvals: (ev.approvals ?? []).map(a => ({
      title: a.authorityTitle,
      decision: a.decision ?? '',
      sign: a.signatureText ?? ''
    })),
    footer: `${candidate.candidateCode} · Interview ${ev.interviewNumber} · filled by ${ev.createdByName ?? EMPTY} on ${new Date(
      ev.createdAtUtc
    ).toLocaleDateString('en-GB')}`
  };
}
