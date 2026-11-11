import type { ProjectStage } from '../../core/api/api-types';
import type { Step } from '../../shared/ui/stepper/stepper';

export const STAGE_LABELS: Record<ProjectStage, string> = {
  Draft: 'Draft',
  FilesUploaded: 'Files uploaded',
};

/** Pill colour class for a stage. */
export function stagePillClass(stage: ProjectStage): string {
  return stage === 'FilesUploaded' ? 'pill-info' : 'pill-warning';
}

/** The five lifecycle stages with their state for a project at <paramref name="stage"/>. Later stages unlock in later slices. */
export function lifecycleSteps(stage: ProjectStage): Step[] {
  const filesDone = stage === 'FilesUploaded';
  return [
    {
      label: 'Files & team',
      caption: filesDone ? 'Uploaded' : 'Upload inputs',
      state: filesDone ? 'done' : 'current',
    },
    { label: 'Analysis', caption: 'Not started', state: filesDone ? 'current' : 'upcoming' },
    { label: 'Architecture', caption: 'Not started', state: 'upcoming' },
    { label: 'Tasks', caption: 'Not started', state: 'upcoming' },
    { label: 'Test cases', caption: 'Not started', state: 'upcoming' },
  ];
}
