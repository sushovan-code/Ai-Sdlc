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
  type TeamMember,
  type TeamOverview,
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

interface RawDetail extends Omit<ProjectDetail, 'files' | 'sprintWeeks'> {
  files: RawFile[];
  sprintWeeks: Numeric;
}

interface RawTeam {
  sprintWeeks: Numeric;
  hoursPerDay: Numeric;
  sprintCount: Numeric;
  totalAllocatedHours: Numeric;
  totalCapacityHours: Numeric;
  members: Record<string, unknown>[];
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

const num = (value: unknown): number => Number(value);

const toMember = (raw: Record<string, unknown>): TeamMember => ({
  id: raw['id'] as string,
  name: raw['name'] as string,
  role: raw['role'] as string,
  skills: raw['skills'] as string[],
  capacityHours: num(raw['capacityHours']),
  availabilityHours: num(raw['availabilityHours']),
  sprintCapacityHours: num(raw['sprintCapacityHours']),
  allocatedHours: num(raw['allocatedHours']),
  totalCapacityHours: num(raw['totalCapacityHours']),
  utilizationPercent: num(raw['utilizationPercent']),
  sprints: (raw['sprints'] as Record<string, unknown>[]).map((s) => ({
    sprint: num(s['sprint']),
    allocatedHours: num(s['allocatedHours']),
    capacityHours: num(s['capacityHours']),
  })),
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
    return this.http.get<RawDetail>(`${this.base}/${projectId}`).pipe(
      map((project) => ({
        ...project,
        sprintWeeks: Number(project.sprintWeeks),
        files: project.files.map(toFile),
      })),
    );
  }

  getTeam(projectId: string): Observable<TeamOverview> {
    return this.http.get<RawTeam>(`${this.base}/${projectId}/team`).pipe(
      map((team) => ({
        sprintWeeks: num(team.sprintWeeks),
        hoursPerDay: num(team.hoursPerDay),
        sprintCount: num(team.sprintCount),
        totalAllocatedHours: num(team.totalAllocatedHours),
        totalCapacityHours: num(team.totalCapacityHours),
        members: team.members.map(toMember),
      })),
    );
  }

  setSprintCadence(projectId: string, weeks: number): Observable<void> {
    return this.http
      .put<unknown>(`${this.base}/${projectId}/sprint-cadence`, { weeks })
      .pipe(map(() => undefined));
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
