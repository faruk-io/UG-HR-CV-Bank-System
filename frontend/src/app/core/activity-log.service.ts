import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './auth.models';
import { ActivityLogOptions, ActivityLogQuery } from './activity-log.models';
import { ReportData } from './report.models';

const API = `${API_BASE_URL}/api/activity-logs`;

@Injectable({ providedIn: 'root' })
export class ActivityLogService {
  private readonly http = inject(HttpClient);

  options(): Observable<ActivityLogOptions> {
    return this.http.get<ActivityLogOptions>(`${API}/options`);
  }

  run(query: ActivityLogQuery): Observable<ReportData> {
    return this.http.get<ReportData>(API, { params: this.toParams(query) });
  }

  excel(query: ActivityLogQuery): Observable<Blob> {
    return this.http.get(`${API}/excel`, { params: this.toParams(query), responseType: 'blob' });
  }

  errorMessage(error: unknown, fallback: string): string {
    const body = (error as { error?: { message?: string } })?.error;
    return body?.message ?? fallback;
  }

  private toParams(query: ActivityLogQuery): HttpParams {
    let params = new HttpParams();

    const set = (key: string, value: string | null) => {
      const trimmed = value?.trim() ?? '';
      if (trimmed) {
        params = params.set(key, trimmed);
      }
    };

    set('dateFrom', query.dateFrom);
    set('dateTo', query.dateTo);
    set('actor', query.actor);
    set('action', query.action);
    set('entityType', query.entityType);
    set('search', query.search);

    return params;
  }
}
