import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { API_BASE } from './api-types';
import type { StageContent, StageKey, StageOverview, StageVersion } from './stage-types';

/** Gates 2 to 4: architecture, tasks and test cases. The server decides who may act; this only calls it. */
@Injectable({ providedIn: 'root' })
export class StageApi {
  private readonly http = inject(HttpClient);

  get(projectId: string, stage: StageKey): Observable<StageOverview> {
    return this.http.get<StageOverview>(this.url(projectId, stage));
  }

  generate(projectId: string, stage: StageKey, feedback: string | null): Observable<StageVersion> {
    return this.http.post<StageVersion>(`${this.url(projectId, stage)}/generate`, { feedback });
  }

  approve(
    projectId: string,
    stage: StageKey,
    version: number,
    comment: string | null,
  ): Observable<void> {
    return this.http
      .post<unknown>(`${this.url(projectId, stage)}/versions/${version}/approve`, { comment })
      .pipe(map(() => undefined));
  }

  reject(projectId: string, stage: StageKey, version: number, comment: string): Observable<void> {
    return this.http
      .post<unknown>(`${this.url(projectId, stage)}/versions/${version}/reject`, { comment })
      .pipe(map(() => undefined));
  }

  /** Saves a person's corrected copy as a new version waiting for review. */
  edit(projectId: string, stage: StageKey, content: StageContent): Observable<StageVersion> {
    return this.http.put<StageVersion>(`${this.url(projectId, stage)}/edit`, content);
  }

  private url(projectId: string, stage: StageKey): string {
    return `${API_BASE}/projects/${projectId}/stages/${stage}`;
  }
}
