import type { ProjectStage } from '../../core/api/api-types';

export const STAGE_LABELS: Record<ProjectStage, string> = {
  Draft: 'Draft',
  FilesUploaded: 'Files uploaded',
};

/** Pill colour class for a stage. */
export function stagePillClass(stage: ProjectStage): string {
  return stage === 'FilesUploaded' ? 'pill-info' : 'pill-warning';
}
