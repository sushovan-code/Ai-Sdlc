import type { ProjectStage } from '../../core/api/api-types';

export const STAGE_LABELS: Record<ProjectStage, string> = {
  Draft: 'Draft',
  FilesUploaded: 'Files uploaded',
  AnalysisApproved: 'Analysis approved',
  ArchitectureApproved: 'Architecture approved',
  TasksApproved: 'Tasks approved',
  TestCasesApproved: 'Complete',
};

/** Pill colour class for a stage. */
export function stagePillClass(stage: ProjectStage): string {
  if (stage !== 'Draft' && stage !== 'FilesUploaded') {
    return 'pill-success';
  }
  return stage === 'FilesUploaded' ? 'pill-info' : 'pill-warning';
}
