export interface AppSettings {
  companyName: string;
  companyAddress: string | null;
  maxUploadMb: number;
  updatedByName: string | null;
  updatedAtUtc: string | null;
}

export interface SettingsUpdateRequest {
  companyName: string;
  companyAddress: string | null;
  maxUploadMb: number;
}

export interface MasterDataItem {
  id: number;
  name: string;
  isActive: boolean;
  usedByRecords: number;
}

export interface MasterDataView {
  concerns: MasterDataItem[];
  designations: MasterDataItem[];
  jobPositions: MasterDataItem[];
}

export type MasterDataKind = 'concerns' | 'designations' | 'job-positions';

/** Which part of the loaded view holds each list; the API URLs use the kind itself. */
export const MASTER_DATA_FIELD: Record<MasterDataKind, keyof MasterDataView> = {
  concerns: 'concerns',
  designations: 'designations',
  'job-positions': 'jobPositions'
};

export const MASTER_DATA_TABS: { kind: MasterDataKind; title: string; hint: string }[] = [
  {
    kind: 'concerns',
    title: 'Concerns',
    hint: 'The business unit a candidate applied under. Used on the candidate form and in reports.'
  },
  {
    kind: 'designations',
    title: 'Designations',
    hint: 'Offered for the "Designation Fixed" field of the interview evaluation form.'
  },
  {
    kind: 'job-positions',
    title: 'Job positions',
    hint: 'The position a candidate applied for. Used on the candidate form and in reports.'
  }
];

export const MAX_UPLOAD_MB = 20;

export const UPLOAD_MB_OPTIONS: number[] = Array.from(
  { length: MAX_UPLOAD_MB },
  (_, index) => MAX_UPLOAD_MB - index
);

export function fallbackSettings(): AppSettings {
  return { companyName: 'Union Group CV Bank', companyAddress: null, maxUploadMb: 10, updatedByName: null, updatedAtUtc: null };
}

export function emptyMasterDataView(): MasterDataView {
  return { concerns: [], designations: [], jobPositions: [] };
}
