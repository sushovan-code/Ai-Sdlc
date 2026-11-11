import type { ProjectStage } from '../../core/api/api-types';

export const STAGE_LABELS: Record<ProjectStage, string> = {
  Draft: 'Draft',
  FilesUploaded: 'Files uploaded',
  AnalysisApproved: 'Analysis approved',
};

/** Pill colour class for a stage. */
export function stagePillClass(stage: ProjectStage): string {
  if (stage === 'AnalysisApproved') {
    return 'pill-success';
  }
  return stage === 'FilesUploaded' ? 'pill-info' : 'pill-warning';
}
