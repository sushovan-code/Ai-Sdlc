/** The five roles of the platform, exactly as the API names them. */
export const AppRole = {
  ProjectManager: 'ProjectManager',
  Architect: 'Architect',
  Developer: 'Developer',
  QA: 'QA',
  Admin: 'Admin',
} as const;

export type AppRole = (typeof AppRole)[keyof typeof AppRole];

export const ALL_ROLES: readonly AppRole[] = Object.values(AppRole);

export const ROLE_LABELS: Record<AppRole, string> = {
  ProjectManager: 'Project Manager',
  Architect: 'Architect',
  Developer: 'Developer',
  QA: 'QA',
  Admin: 'Administrator',
};

export function roleLabel(role: string): string {
  return (ROLE_LABELS as Record<string, string>)[role] ?? role;
}
