import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { API_BASE_URL, CurrentUser, LoginResponse } from './auth.models';

const TOKEN_KEY = 'cvbank.token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  readonly currentUser = signal<CurrentUser | null>(null);

  readonly isAuthenticated = computed(() => !!this.currentUser());

  get token(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  login(userName: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${API_BASE_URL}/api/auth/login`, { userName, password }).pipe(
      tap(response => {
        localStorage.setItem(TOKEN_KEY, response.token);
        this.currentUser.set({
          userId: response.userId,
          userName,
          fullName: response.fullName,
          role: response.role,
          permissions: response.permissions
        });
      })
    );
  }

  loadCurrentUser(): Observable<CurrentUser> {
    return this.http.get<CurrentUser>(`${API_BASE_URL}/api/auth/me`).pipe(
      tap(user => this.currentUser.set(user))
    );
  }

  can(permission: string): boolean {
    return this.currentUser()?.permissions.includes(permission) ?? false;
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    this.currentUser.set(null);
    this.router.navigateByUrl('/login');
  }
}
