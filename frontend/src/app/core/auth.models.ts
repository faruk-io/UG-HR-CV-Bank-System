import { environment } from '../../environments/environment';

export const API_BASE_URL = environment.apiBaseUrl;

export interface LoginResponse {
  token: string;
  userId: number;
  fullName: string;
  role: string;
  permissions: string[];
  expiresAtUtc: string;
}

export interface CurrentUser {
  userId: number;
  userName: string;
  fullName: string;
  role: string;
  permissions: string[];
}
