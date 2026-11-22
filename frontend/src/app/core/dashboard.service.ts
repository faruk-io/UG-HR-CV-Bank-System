import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './auth.models';
import { CandidateSummary, DashboardSummary } from './dashboard.models';

const API = `${API_BASE_URL}/api`;

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);

  summary(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`${API}/dashboard/summary`);
  }

  candidateSummary(candidateId: number): Observable<CandidateSummary> {
    return this.http.get<CandidateSummary>(`${API}/candidates/${candidateId}/summary`);
  }

  errorMessage(error: unknown, fallback = 'Could not reach the server. Please try again.'): string {
    const body = (error as { error?: { message?: string } })?.error;
    return body?.message ?? fallback;
  }
}
