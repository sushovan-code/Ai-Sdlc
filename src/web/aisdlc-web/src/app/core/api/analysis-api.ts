import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import type { AnalysisOverview, AnalysisVersion } from './analysis-types';
import { API_BASE } from './api-types';

/** The gate 1 requirements analysis. The server decides who may generate and decide; this only calls it. */
@Injectable({ providedIn: 'root' })
export class AnalysisApi {
  private readonly http = inject(HttpClient);

  get(projectId: string): Observable<AnalysisOverview> {
    return this.http.get<AnalysisOverview>(this.url(projectId));
  }

  /** Drafts a new version. The feedback, when given, steers the redraft. */
  generate(projectId: string, feedback: string | null): Observable<AnalysisVersion> {
    return this.http.post<AnalysisVersion>(`${this.url(projectId)}/generate`, { feedback });
  }

  approve(projectId: string, version: number, comment: string | null): Observable<void> {
    return this.http
      .post<unknown>(`${this.url(projectId)}/versions/${version}/approve`, { comment })
      .pipe(map(() => undefined));
  }

  reject(projectId: string, version: number, comment: string): Observable<void> {
    return this.http
      .post<unknown>(`${this.url(projectId)}/versions/${version}/reject`, { comment })
      .pipe(map(() => undefined));
  }

  private url(projectId: string): string {
    return `${API_BASE}/projects/${projectId}/analysis`;
  }
}
