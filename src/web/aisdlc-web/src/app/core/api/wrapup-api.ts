import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from './api-types';
import type { AuditPage, Traceability } from './wrapup-types';

/** Traceability, the audit trail and the report of a project. Any member can read them. */
@Injectable({ providedIn: 'root' })
export class WrapupApi {
  private readonly http = inject(HttpClient);

  traceability(projectId: string): Observable<Traceability> {
    return this.http.get<Traceability>(`${this.url(projectId)}/traceability`);
  }

  audit(projectId: string, page: number, pageSize: number): Observable<AuditPage> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<AuditPage>(`${this.url(projectId)}/audit`, { params });
  }

  report(projectId: string): Observable<Blob> {
    return this.http.get(`${this.url(projectId)}/report`, { responseType: 'blob' });
  }

  private url(projectId: string): string {
    return `${API_BASE}/projects/${projectId}`;
  }
}
