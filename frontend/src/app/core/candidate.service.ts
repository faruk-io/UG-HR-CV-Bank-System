import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './auth.models';
import {
  Candidate,
  CandidateDocument,
  CandidateListItem,
  CandidateQuery,
  CandidateSaveRequest,
  DuplicateCandidate,
  MasterDataOption,
  PagedResult
} from './candidate.models';

const API = `${API_BASE_URL}/api`;

function messageOf(error: unknown, fallback: string): string {
  const body = (error as { error?: { message?: string } })?.error;
  return body?.message ?? fallback;
}

@Injectable({ providedIn: 'root' })
export class CandidateService {
  private readonly http = inject(HttpClient);

  list(query: CandidateQuery): Observable<PagedResult<CandidateListItem>> {
    return this.http.get<PagedResult<CandidateListItem>>(`${API}/candidates`, { params: this.toParams(query) });
  }

  get(id: number): Observable<Candidate> {
    return this.http.get<Candidate>(`${API}/candidates/${id}`);
  }

  create(request: CandidateSaveRequest): Observable<Candidate> {
    return this.http.post<Candidate>(`${API}/candidates`, request);
  }

  update(id: number, request: CandidateSaveRequest): Observable<Candidate> {
    return this.http.put<Candidate>(`${API}/candidates/${id}`, request);
  }

  setStatus(id: number, status: number): Observable<Candidate> {
    return this.http.put<Candidate>(`${API}/candidates/${id}/status`, { status });
  }

  remove(id: number): Observable<void> {
    return this.http.delete<void>(`${API}/candidates/${id}`);
  }

  findDuplicates(mobileNumber: string | null, email: string | null, excludeId?: number): Observable<DuplicateCandidate[]> {
    let params = new HttpParams();
    if (mobileNumber) params = params.set('mobileNumber', mobileNumber);
    if (email) params = params.set('email', email);
    if (excludeId) params = params.set('excludeId', excludeId);

    return this.http.get<DuplicateCandidate[]>(`${API}/candidates/duplicates`, { params });
  }

  uploadDocument(
    candidateId: number,
    file: File,
    category: number,
    remarks: string | null,
    replaceCurrent: boolean
  ): Observable<CandidateDocument> {
    const form = new FormData();
    form.append('file', file, file.name);
    form.append('category', String(category));
    form.append('remarks', remarks ?? '');
    form.append('replaceCurrent', String(replaceCurrent));

    return this.http.post<CandidateDocument>(`${API}/candidates/${candidateId}/documents`, form);
  }

  withdrawDocument(documentId: number): Observable<void> {
    return this.http.delete<void>(`${API}/candidates/documents/${documentId}`);
  }

  fetchDocument(documentId: number, inline: boolean): Observable<Blob> {
    return this.http.get(`${API}/candidates/documents/${documentId}/download`, {
      params: new HttpParams().set('inline', inline),
      responseType: 'blob'
    });
  }

  concerns(): Observable<MasterDataOption[]> {
    return this.http.get<MasterDataOption[]>(`${API}/master-data/concerns`);
  }

  jobPositions(): Observable<MasterDataOption[]> {
    return this.http.get<MasterDataOption[]>(`${API}/master-data/job-positions`);
  }

  designations(): Observable<MasterDataOption[]> {
    return this.http.get<MasterDataOption[]>(`${API}/master-data/designations`);
  }

  addConcern(name: string): Observable<MasterDataOption> {
    return this.http.post<MasterDataOption>(`${API}/master-data/concerns`, { name });
  }

  addJobPosition(name: string): Observable<MasterDataOption> {
    return this.http.post<MasterDataOption>(`${API}/master-data/job-positions`, { name });
  }

  addDesignation(name: string): Observable<MasterDataOption> {
    return this.http.post<MasterDataOption>(`${API}/master-data/designations`, { name });
  }

  errorMessage(error: unknown, fallback: string): string {
    return messageOf(error, fallback);
  }

  private toParams(query: CandidateQuery): HttpParams {
    let params = new HttpParams();

    const set = (key: string, value: unknown) => {
      if (value !== null && value !== undefined && value !== '') {
        params = params.set(key, String(value));
      }
    };

    set('search', query.search?.trim() ?? null);
    set('status', query.status);
    set('jobPositionId', query.jobPositionId);
    set('concernId', query.concernId);
    set('minExperience', query.minExperience);
    set('maxExperience', query.maxExperience);
    set('hasCv', query.hasCv);
    set('page', query.page ?? 1);
    set('pageSize', query.pageSize ?? 20);

    return params;
  }
}
