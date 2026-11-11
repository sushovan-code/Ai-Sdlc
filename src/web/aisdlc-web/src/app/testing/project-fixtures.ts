import type {
  ProjectDetail,
  ProjectFile,
  ProjectMember,
  TeamMember,
  TeamOverview,
} from '../core/api/api-types';
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
    sprintWeeks: 2,
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

export function makeTeamMember(overrides: Partial<TeamMember> = {}): TeamMember {
  return {
    id: 't1111111-1111-1111-1111-111111111111',
    name: 'Asha Rao',
    role: 'Developer',
    skills: ['C#', 'SQL'],
    capacityHours: 40,
    availabilityHours: 30,
    sprintCapacityHours: 30,
    allocatedHours: 0,
    totalCapacityHours: 30,
    utilizationPercent: 0,
    sprints: [{ sprint: 1, allocatedHours: 0, capacityHours: 30 }],
    ...overrides,
  };
}

export function makeTeam(overrides: Partial<TeamOverview> = {}): TeamOverview {
  const members = overrides.members ?? [makeTeamMember()];
  return {
    sprintWeeks: 2,
    hoursPerDay: 8,
    sprintCount: 1,
    totalAllocatedHours: 0,
    totalCapacityHours: members.reduce((sum, m) => sum + m.totalCapacityHours, 0),
    members,
    ...overrides,
  };
}

/** The team response for a project with no team sheet yet. */
export const NO_TEAM: TeamOverview = makeTeam({ members: [], totalCapacityHours: 0 });
