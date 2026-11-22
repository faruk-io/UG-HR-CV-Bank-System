export interface UserListItem {
  id: number;
  fullName: string;
  userName: string;
  email: string | null;
  mobileNumber: string | null;
  roleId: number;
  roleCode: string;
  roleName: string;
  isActive: boolean;
  createdAtUtc: string;
  lastLoginUtc: string | null;
  activityCount: number;
}

export interface RoleOption {
  id: number;
  code: string;
  name: string;
  permissions: string[];
}

export interface UserQuery {
  search: string | null;
  roleId: number | null;
  isActive: boolean | null;
  page: number;
  pageSize: number;
}

export interface UserSaveRequest {
  fullName: string;
  userName?: string;
  email: string | null;
  mobileNumber: string | null;
  roleId: number;
  isActive: boolean;
  password?: string;
  newPassword?: string | null;
}

export const PERMISSION_LABELS: Record<string, string> = {
  'candidate.view': 'View candidates',
  'candidate.create': 'Create candidates',
  'candidate.update': 'Update candidates',
  'candidate.delete': 'Delete candidates',
  'cv.upload': 'Upload CV',
  'cv.download': 'Download CV',
  'evaluation.view': 'View interviews',
  'evaluation.generate': 'Create interviews',
  'evaluation.upload': 'Upload scanned form',
  'evaluation.manage': 'Manage interviews',
  'report.view': 'Run reports',
  'user.manage': 'Manage users',
  'settings.manage': 'Manage settings',
  'activity.log.view': 'View activity log',
  'document.manage': 'Manage documents'
};

export function permissionLabel(code: string): string {
  return PERMISSION_LABELS[code] ?? code;
}

export function blankUserQuery(): UserQuery {
  return { search: null, roleId: null, isActive: null, page: 1, pageSize: 20 };
}
