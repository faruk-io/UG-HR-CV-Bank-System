import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './auth.models';
import { CandidateDocument, PagedResult } from './candidate.models';
import {
  CandidateEvaluationHistoryItem,
  Evaluation,
  EvaluationListItem,
  EvaluationQuery,
  EvaluationSaveRequest
} from './evaluation.models';

const API = `${API_BASE_URL}/api`;

@Injectable({ providedIn: 'root' })
export class EvaluationService {
  private readonly http = inject(HttpClient);

  list(query: EvaluationQuery): Observable<PagedResult<EvaluationListItem>> {
    return this.http.get<PagedResult<EvaluationListItem>>(`${API}/evaluations`, { params: this.toParams(query) });
  }

  history(candidateId: number): Observable<CandidateEvaluationHistoryItem[]> {
    return this.http.get<CandidateEvaluationHistoryItem[]>(`${API}/evaluations/by-candidate/${candidateId}`);
  }

  get(id: number): Observable<Evaluation> {
    return this.http.get<Evaluation>(`${API}/evaluations/${id}`);
  }

  create(request: EvaluationSaveRequest): Observable<Evaluation> {
    return this.http.post<Evaluation>(`${API}/evaluations`, request);
  }

  update(id: number, request: EvaluationSaveRequest): Observable<Evaluation> {
    return this.http.put<Evaluation>(`${API}/evaluations/${id}`, request);
  }

  remove(id: number): Observable<void> {
    return this.http.delete<void>(`${API}/evaluations/${id}`);
  }

  uploadScan(id: number, file: File, remarks: string | null): Observable<CandidateDocument> {
    const form = new FormData();
    form.append('file', file, file.name);
    form.append('remarks', remarks ?? '');

    return this.http.post<CandidateDocument>(`${API}/evaluations/${id}/scan`, form);
  }

  downloadScan(documentId: number, inline: boolean): Observable<Blob> {
    return this.http.get(`${API}/evaluations/scans/${documentId}/download`, {
      params: new HttpParams().set('inline', inline),
      responseType: 'blob'
    });
  }

  errorMessage(error: unknown, fallback: string): string {
    const body = (error as { error?: { message?: string } })?.error;
    return body?.message ?? fallback;
  }

  private toParams(query: EvaluationQuery): HttpParams {
    let params = new HttpParams();

    const set = (key: string, value: unknown) => {
      if (value !== null && value !== undefined && value !== '') {
        params = params.set(key, String(value));
      }
    };

    set('search', query.search?.trim() ?? null);
    set('candidateId', query.candidateId);
    set('jobPositionId', query.jobPositionId);
    set('status', query.status);
    set('decision', query.decision);
    set('dateFrom', query.dateFrom);
    set('dateTo', query.dateTo);
    set('hasScannedForm', query.hasScannedForm);
    set('page', query.page ?? 1);
    set('pageSize', query.pageSize ?? 20);

    return params;
  }
}
