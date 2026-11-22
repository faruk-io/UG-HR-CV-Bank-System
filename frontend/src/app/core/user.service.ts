import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './auth.models';
import { PagedResult } from './candidate.models';
import { RoleOption, UserListItem, UserQuery, UserSaveRequest } from './user.models';

const API = `${API_BASE_URL}/api`;

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly http = inject(HttpClient);

  list(query: UserQuery): Observable<PagedResult<UserListItem>> {
    return this.http.get<PagedResult<UserListItem>>(`${API}/users`, { params: this.toParams(query) });
  }

  roles(): Observable<RoleOption[]> {
    return this.http.get<RoleOption[]>(`${API}/users/roles`);
  }

  create(request: UserSaveRequest): Observable<UserListItem> {
    return this.http.post<UserListItem>(`${API}/users`, request);
  }

  update(id: number, request: UserSaveRequest): Observable<UserListItem> {
    return this.http.put<UserListItem>(`${API}/users/${id}`, request);
  }

  remove(id: number): Observable<void> {
    return this.http.delete<void>(`${API}/users/${id}`);
  }

  changeOwnPassword(currentPassword: string, newPassword: string): Observable<void> {
    return this.http.post<void>(`${API}/auth/change-password`, { currentPassword, newPassword });
  }

  errorMessage(error: unknown, fallback: string): string {
    const body = (error as { error?: { message?: string } })?.error;
    return body?.message ?? fallback;
  }

  private toParams(query: UserQuery): HttpParams {
    let params = new HttpParams();

    const set = (key: string, value: unknown) => {
      if (value !== null && value !== undefined && value !== '') {
        params = params.set(key, String(value));
      }
    };

    set('search', query.search?.trim() ?? null);
    set('roleId', query.roleId);
    set('isActive', query.isActive);
    set('page', query.page ?? 1);
    set('pageSize', query.pageSize ?? 20);

    return params;
  }
}
