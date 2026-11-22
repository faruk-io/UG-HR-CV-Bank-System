import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './auth.models';
import { ReportData, ReportDefinition, ReportQuery } from './report.models';

const API = `${API_BASE_URL}/api`;

@Injectable({ providedIn: 'root' })
export class ReportService {
  private readonly http = inject(HttpClient);

  catalog(): Observable<ReportDefinition[]> {
    return this.http.get<ReportDefinition[]>(`${API}/reports`);
  }

  run(code: string, query: ReportQuery): Observable<ReportData> {
    return this.http.get<ReportData>(`${API}/reports/${code}`, { params: this.toParams(query) });
  }

  excel(code: string, query: ReportQuery): Observable<Blob> {
    return this.http.get(`${API}/reports/${code}/excel`, {
      params: this.toParams(query),
      responseType: 'blob'
    });
  }

  errorMessage(error: unknown, fallback: string): string {
    const body = (error as { error?: { message?: string } })?.error;
    return body?.message ?? fallback;
  }

  private toParams(query: ReportQuery): HttpParams {
    let params = new HttpParams();

    const set = (key: string, value: unknown) => {
      if (value !== null && value !== undefined && value !== '') {
        params = params.set(key, String(value));
      }
    };

    set('dateFrom', query.dateFrom);
    set('dateTo', query.dateTo);
    set('search', query.search?.trim() ?? null);
    set('jobPositionId', query.jobPositionId);
    set('concernId', query.concernId);
    set('status', query.status);
    set('cvOnFile', query.cvOnFile);
    set('groupBy', query.groupBy);
    set('interviewView', query.interviewView);

    return params;
  }
}
