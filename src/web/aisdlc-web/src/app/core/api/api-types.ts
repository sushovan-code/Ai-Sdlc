import type { components } from './schema';

/** API contract types, generated from the backend OpenAPI document (`npm run api:types`). */
export type UserDto = components['schemas']['UserDto'];
export type AuthTokenDto = components['schemas']['AuthTokenDto'];
export type RegisterRequest = components['schemas']['RegisterRequest'];
export type LoginRequest = components['schemas']['LoginRequest'];
export type ChangePasswordRequest = components['schemas']['ChangePasswordRequest'];
export type SetUserRoleRequest = components['schemas']['SetUserRoleRequest'];

/** A page of users. The OpenAPI document types integers as `number | string`; the client normalises them. */
export interface UserPage {
  items: UserDto[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export const API_BASE = '/api/v1';

export type FileKind = components['schemas']['FileKind'];
export type ProjectStage = components['schemas']['ProjectStage'];
export type ProjectMember = components['schemas']['ProjectMemberDto'];
export type CreateProjectRequest = components['schemas']['CreateProjectRequest'];

export interface Page<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface ProjectSummary {
  id: string;
  name: string;
  description: string | null;
  stage: ProjectStage;
  createdAtUtc: string;
  memberCount: number;
  hasRequirements: boolean;
  hasTeamTemplate: boolean;
}

export interface ProjectFile {
  id: string;
  kind: FileKind;
  version: number;
  fileName: string;
  sizeBytes: number;
  sha256: string;
  uploadedAtUtc: string;
  uploadedById: string;
  uploadedByName: string;
  isLatest: boolean;
}

export interface ProjectDetail {
  id: string;
  name: string;
  description: string | null;
  stage: ProjectStage;
  createdAtUtc: string;
  createdById: string;
  canManage: boolean;
  members: ProjectMember[];
  files: ProjectFile[];
}

/** Route segment the API uses for each kind of file. */
export const FILE_KIND_ROUTES: Record<FileKind, string> = {
  Requirements: 'requirements',
  TeamTemplate: 'team-template',
};
