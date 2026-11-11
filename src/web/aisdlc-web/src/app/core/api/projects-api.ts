import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import {
  API_BASE,
  FILE_KIND_ROUTES,
  type CreateProjectRequest,
  type FileKind,
  type Page,
  type ProjectDetail,
  type ProjectFile,
  type ProjectMember,
  type ProjectSummary,
  type UserDto,
} from './api-types';

/** Integers arrive typed as `number | string` in the OpenAPI document, so they are normalised once, here. */
type Numeric = number | string;

interface RawFile extends Omit<ProjectFile, 'version' | 'sizeBytes'> {
  version: Numeric;
  sizeBytes: Numeric;
}

interface RawSummary extends Omit<ProjectSummary, 'memberCount'> {
  memberCount: Numeric;
}

interface RawDetail extends Omit<ProjectDetail, 'files'> {
  files: RawFile[];
}

interface RawPage<T> {
  items: T[];
  page: Numeric;
  pageSize: Numeric;
  totalCount: Numeric;
}

const toFile = (file: RawFile): ProjectFile => ({
  ...file,
  version: Number(file.version),
  sizeBytes: Number(file.sizeBytes),
});

const toSummary = (project: RawSummary): ProjectSummary => ({
  ...project,
  memberCount: Number(project.memberCount),
});

/** Projects, their members and their input files. The server decides who may do what; this only calls it. */
@Injectable({ providedIn: 'root' })
export class ProjectsApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${API_BASE}/projects`;

  list(page = 1, pageSize = 24): Observable<Page<ProjectSummary>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<RawPage<RawSummary>>(this.base, { params }).pipe(
      map((body) => ({
        items: body.items.map(toSummary),
        page: Number(body.page),
        pageSize: Number(body.pageSize),
        totalCount: Number(body.totalCount),
      })),
    );
  }

  get(projectId: string): Observable<ProjectDetail> {
    return this.http
      .get<RawDetail>(`${this.base}/${projectId}`)
      .pipe(map((project) => ({ ...project, files: project.files.map(toFile) })));
  }

  create(request: CreateProjectRequest): Observable<ProjectSummary> {
    return this.http.post<RawSummary>(this.base, request).pipe(map(toSummary));
  }

  addMember(projectId: string, userId: string): Observable<ProjectMember> {
    return this.http.post<ProjectMember>(`${this.base}/${projectId}/members`, { userId });
  }

  removeMember(projectId: string, userId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${projectId}/members/${userId}`);
  }

  upload(projectId: string, kind: FileKind, file: File): Observable<ProjectFile> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http
      .post<RawFile>(`${this.base}/${projectId}/files/${FILE_KIND_ROUTES[kind]}`, form)
      .pipe(map(toFile));
  }

  download(projectId: string, fileId: string): Observable<Blob> {
    return this.http.get(`${this.base}/${projectId}/files/${fileId}/download`, {
      responseType: 'blob',
    });
  }

  deleteFile(projectId: string, fileId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${projectId}/files/${fileId}`);
  }

  /** People finder for the member picker (Project Managers only on the server). */
  searchPeople(query: string): Observable<UserDto[]> {
    const params = new HttpParams().set('query', query);
    return this.http.get<UserDto[]>(`${API_BASE}/directory/users`, { params });
  }
}
