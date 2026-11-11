import type { ArtifactStatus, CriticNote, GuardrailFlag, ReviewDecision } from './analysis-types';

/** Route segment the API uses for each reviewed stage after the analysis. */
export type StageKey = 'architecture' | 'tasks' | 'test-cases';

export interface StageItem {
  id: string;
  title: string;
  text: string;
  tags: string[];
  /** Ids of the earlier items this one traces back to. */
  refs: string[];
}

export interface StageSection {
  title: string;
  items: StageItem[];
}

export interface StageContent {
  summary: string;
  sections: StageSection[];
}

export interface PlannedAssignment {
  taskId: string;
  memberId: string;
  memberName: string;
  sprint: number;
  hours: number;
}

export interface StageNotes {
  critic: CriticNote[];
  flags: GuardrailFlag[];
  assignments: PlannedAssignment[];
}

export interface StageVersion {
  version: number;
  status: ArtifactStatus;
  source: 'Ai' | 'Human';
  model: string;
  promptVersion: string;
  feedback: string | null;
  basedOnVersion: number | null;
  createdAtUtc: string;
  content: StageContent;
  notes: StageNotes;
  decisions: ReviewDecision[];
}

export interface StageOverview {
  /** The role that generates, edits and decides this stage. */
  approverRole: string;
  canGenerate: boolean;
  blockedReason: string | null;
  /** Newest first. */
  versions: StageVersion[];
}
