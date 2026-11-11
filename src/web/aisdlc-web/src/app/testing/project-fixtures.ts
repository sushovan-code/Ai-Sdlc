import type { ProjectDetail, ProjectFile, ProjectMember } from '../core/api/api-types';
import { AppRole } from '../core/auth/roles';

export const PROJECT_ID = 'p1111111-1111-1111-1111-111111111111';

export function makeMember(overrides: Partial<ProjectMember> = {}): ProjectMember {
  return {
    userId: '11111111-1111-1111-1111-111111111111',
    displayName: 'Demo Project Manager',
    email: 'pm@demo.local',
    role: AppRole.ProjectManager,
    isOwner: true,
    ...overrides,
  };
}

export function makeFile(overrides: Partial<ProjectFile> = {}): ProjectFile {
  return {
    id: 'f1111111-1111-1111-1111-111111111111',
    kind: 'Requirements',
    version: 1,
    fileName: 'requirements.docx',
    sizeBytes: 2048,
    sha256: 'ABCDEF0123456789'.repeat(4),
    uploadedAtUtc: '2026-11-11T09:00:00Z',
    uploadedById: '11111111-1111-1111-1111-111111111111',
    uploadedByName: 'Demo Project Manager',
    isLatest: true,
    ...overrides,
  };
}

export function makeProject(overrides: Partial<ProjectDetail> = {}): ProjectDetail {
  return {
    id: PROJECT_ID,
    name: 'Express Checkout',
    description: 'Faster pay',
    stage: 'Draft',
    createdAtUtc: '2026-11-11T09:00:00Z',
    createdById: '11111111-1111-1111-1111-111111111111',
    canManage: true,
    members: [makeMember()],
    files: [],
    ...overrides,
  };
}

/** The API sends integers as strings in some cases; this is how a file looks on the wire. */
export function onTheWire(file: ProjectFile): Record<string, unknown> {
  return { ...file, version: String(file.version), sizeBytes: String(file.sizeBytes) };
}
