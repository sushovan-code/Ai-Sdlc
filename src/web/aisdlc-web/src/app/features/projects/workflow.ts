import type { ProjectStage } from '../../core/api/api-types';
import { AppRole, roleLabel } from '../../core/auth/roles';
import type { Step } from '../../shared/ui/stepper/stepper';

export type StepKey = 'files' | 'analysis' | 'architecture' | 'tasks' | 'tests' | 'wrapup';

export interface Gate {
  number: number;
  name: string;
  approver: AppRole;
}

export interface WorkflowStep {
  key: StepKey;
  label: string;
  /** What this stage does, in one sentence. */
  summary: string;
  /** The human review needed before the project may leave this stage. The first stage has none. */
  gate: Gate | null;
}

/** The lifecycle stages in order: the first and last have no gate, the four in between are each closed by a human review gate. */
export const WORKFLOW: readonly WorkflowStep[] = [
  {
    key: 'files',
    label: 'Files & team',
    summary: 'Upload the requirements and the team template, and choose who works on the project.',
    gate: null,
  },
  {
    key: 'analysis',
    label: 'Analysis',
    summary:
      'AI analyses the requirements into functional and non-functional requirements, ambiguities, gaps and risks.',
    gate: { number: 1, name: 'Requirements analysis review', approver: AppRole.ProjectManager },
  },
  {
    key: 'architecture',
    label: 'Architecture',
    summary:
      'AI drafts an architecture plan that the Software Architect edits, comments on and approves.',
    gate: { number: 2, name: 'Architecture approval', approver: AppRole.Architect },
  },
  {
    key: 'tasks',
    label: 'Tasks',
    summary:
      'AI breaks the approved architecture into development tasks and schedules them across sprints.',
    gate: { number: 3, name: 'Task review and assignment', approver: AppRole.ProjectManager },
  },
  {
    key: 'tests',
    label: 'Test cases',
    summary: 'AI generates QA test cases traced back to the requirements and the tasks.',
    gate: { number: 4, name: 'Test-case approval', approver: AppRole.QA },
  },
  {
    key: 'wrapup',
    label: 'Wrap-up',
    summary:
      'Trace every requirement to its design, tasks and tests, then review the audit trail and take the report.',
    gate: null,
  },
];

/** The furthest stage a project can currently be opened at. Later review stages extend this. */
export function furthestStep(stage: ProjectStage): number {
  switch (stage) {
    case 'TestCasesApproved':
      return 5;
    case 'TasksApproved':
      return 4;
    case 'ArchitectureApproved':
      return 3;
    case 'AnalysisApproved':
      return 2;
    case 'FilesUploaded':
      return 1;
    default:
      return 0;
  }
}

/** Why the user cannot move forward from <paramref name="index"/>, or null when they can. */
export function blockReason(stage: ProjectStage, index: number): string | null {
  if (index >= WORKFLOW.length - 1) {
    return 'This is the last stage.';
  }
  if (index < furthestStep(stage)) {
    return null;
  }
  const gate = WORKFLOW[index].gate;
  if (!gate) {
    return 'Upload both the requirements and the team template to continue.';
  }
  return `Gate ${gate.number} (${gate.name}) must be approved by the ${roleLabel(gate.approver)} first.`;
}

/** The stepper's view of the workflow for a project at <paramref name="stage"/>. */
export function stepsFor(stage: ProjectStage): Step[] {
  const furthest = furthestStep(stage);
  return WORKFLOW.map((step, index) => {
    if (index < furthest) {
      return { label: step.label, caption: 'Done', state: 'done', reachable: true };
    }
    if (index === furthest) {
      return {
        label: step.label,
        caption: index === 0 ? 'Upload inputs' : 'In progress',
        state: 'current',
        reachable: true,
      };
    }
    return { label: step.label, caption: 'Locked', state: 'upcoming', reachable: false };
  });
}
