import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, switchMap } from 'rxjs';
import { API_BASE_URL } from './auth.models';
import { MasterDataKind, MasterDataView, fallbackSettings, AppSettings, SettingsUpdateRequest } from './settings.models';

const API = `${API_BASE_URL}/api`;

interface MasterDataOption {
  id: number;
  name: string;
}

@Injectable({ providedIn: 'root' })
export class SettingsService {
  private readonly http = inject(HttpClient);
  private requested = false;

  /** Values every signed-in screen shows: brand name in the menu, address on printed sheets, upload limit in file pickers. */
  readonly display = signal<AppSettings>(fallbackSettings());

  /** Loads the shared values once per session; safe to call from several screens. */
  ensureDisplay(): void {
    if (this.requested) {
      return;
    }

    this.requested = true;
    this.http.get<AppSettings>(`${API}/settings/display`).subscribe({
      next: value => this.display.set(value),
      error: () => this.requested = false
    });
  }

  view(): Observable<AppSettings> {
    return this.http.get<AppSettings>(`${API}/settings`);
  }

  save(request: SettingsUpdateRequest): Observable<AppSettings> {
    return this.http.put<AppSettings>(`${API}/settings`, request).pipe(
      switchMap(saved => {
        this.display.set(saved);
        return [saved];
      })
    );
  }

  masterData(): Observable<MasterDataView> {
    return this.http.get<MasterDataView>(`${API}/master-data/manage`);
  }

  addMasterData(kind: MasterDataKind, name: string): Observable<MasterDataView> {
    return this.http.post<MasterDataOption>(`${API}/master-data/${kind}`, { name }).pipe(
      switchMap(() => this.masterData())
    );
  }

  renameMasterData(kind: MasterDataKind, id: number, name: string): Observable<MasterDataView> {
    return this.http.put<MasterDataView>(`${API}/master-data/manage/${kind}/${id}`, { name });
  }

  setMasterDataActive(kind: MasterDataKind, id: number, isActive: boolean): Observable<MasterDataView> {
    return this.http.put<MasterDataView>(`${API}/master-data/manage/${kind}/${id}/active`, { isActive });
  }

  errorMessage(error: unknown, fallback: string): string {
    const body = (error as { error?: { message?: string } })?.error;
    return body?.message ?? fallback;
  }
}
