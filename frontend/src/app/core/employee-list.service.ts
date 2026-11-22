import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './auth.models';
import { MasterDataOption, PagedResult } from './candidate.models';
import { ReportData } from './report.models';
import {
  EmployeeListQuery,
  JoiningRecord,
  JoiningSaveRequest,
  ResignedRecord,
  ResignedSaveRequest
} from './employee-list.models';

const API = `${API_BASE_URL}/api`;

function messageOf(error: unknown, fallback: string): string {
  const body = (error as { error?: { message?: string } })?.error;
  return body?.message ?? fallback;
}

@Injectable({ providedIn: 'root' })
export class EmployeeListService {
  private readonly http = inject(HttpClient);

  listJoining(query: EmployeeListQuery): Observable<PagedResult<JoiningRecord>> {
    return this.http.get<PagedResult<JoiningRecord>>(`${API}/joining`, { params: this.toParams(query) });
  }

  createJoining(request: JoiningSaveRequest): Observable<JoiningRecord> {
    return this.http.post<JoiningRecord>(`${API}/joining`, request);
  }

  updateJoining(id: number, request: JoiningSaveRequest): Observable<JoiningRecord> {
    return this.http.put<JoiningRecord>(`${API}/joining/${id}`, request);
  }

  removeJoining(id: number): Observable<void> {
    return this.http.delete<void>(`${API}/joining/${id}`);
  }

  exportJoining(query: EmployeeListQuery): Observable<ReportData> {
    return this.http.get<ReportData>(`${API}/joining/export`, { params: this.toParams(query) });
  }

  excelJoining(query: EmployeeListQuery): Observable<Blob> {
    return this.http.get(`${API}/joining/excel`, { params: this.toParams(query), responseType: 'blob' });
  }

  listResigned(query: EmployeeListQuery): Observable<PagedResult<ResignedRecord>> {
    return this.http.get<PagedResult<ResignedRecord>>(`${API}/resigned`, { params: this.toParams(query) });
  }

  createResigned(request: ResignedSaveRequest): Observable<ResignedRecord> {
    return this.http.post<ResignedRecord>(`${API}/resigned`, request);
  }

  updateResigned(id: number, request: ResignedSaveRequest): Observable<ResignedRecord> {
    return this.http.put<ResignedRecord>(`${API}/resigned/${id}`, request);
  }

  removeResigned(id: number): Observable<void> {
    return this.http.delete<void>(`${API}/resigned/${id}`);
  }

  exportResigned(query: EmployeeListQuery): Observable<ReportData> {
    return this.http.get<ReportData>(`${API}/resigned/export`, { params: this.toParams(query) });
  }

  excelResigned(query: EmployeeListQuery): Observable<Blob> {
    return this.http.get(`${API}/resigned/excel`, { params: this.toParams(query), responseType: 'blob' });
  }

  concerns(): Observable<MasterDataOption[]> {
    return this.http.get<MasterDataOption[]>(`${API}/master-data/concerns`);
  }

  addConcern(name: string): Observable<MasterDataOption> {
    return this.http.post<MasterDataOption>(`${API}/master-data/concerns`, { name });
  }

  errorMessage(error: unknown, fallback: string): string {
    return messageOf(error, fallback);
  }

  private toParams(query: EmployeeListQuery): HttpParams {
    let params = new HttpParams();

    if (query.search?.trim()) {
      params = params.set('search', query.search.trim());
    }

    if (query.concernId !== null && query.concernId !== undefined) {
      params = params.set('concernId', String(query.concernId));
    }

    if (query.dateFrom) {
      params = params.set('dateFrom', query.dateFrom);
    }

    if (query.dateTo) {
      params = params.set('dateTo', query.dateTo);
    }

    params = params.set('page', String(query.page ?? 1));
    params = params.set('pageSize', String(query.pageSize ?? 20));

    return params;
  }
}