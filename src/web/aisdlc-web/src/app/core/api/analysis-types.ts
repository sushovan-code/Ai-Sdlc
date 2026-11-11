/** The requirements analysis as the API returns it (gate 1). */

export type ArtifactStatus = 'PendingReview' | 'Approved' | 'Rejected' | 'Superseded';
export type ReviewDecisionKind = 'Approved' | 'Rejected';

export interface RequirementItem {
  id: string;
  title: string;
  description: string;
  priority: string;
  sourceRefs: string[];
}

export interface QuestionItem {
  id: string;
  text: string;
  sourceRefs: string[];
}

export interface RiskItem {
  id: string;
  description: string;
  severity: string;
  sourceRefs: string[];
}

/** A quote from the uploaded document, found by its chunk reference such as C3. */
export interface Evidence {
  ref: string;
  quote: string;
}

export interface AnalysisContent {
  summary: string;
  functionalRequirements: RequirementItem[];
  nonFunctionalRequirements: RequirementItem[];
  ambiguities: QuestionItem[];
  gaps: QuestionItem[];
  assumptions: string[];
  risks: RiskItem[];
  evidence: Evidence[];
}

export interface CriticNote {
  severity: string;
  note: string;
}

export interface GuardrailFlag {
  code: string;
  message: string;
}

export interface AnalysisNotes {
  critic: CriticNote[];
  flags: GuardrailFlag[];
}

export interface ReviewDecision {
  kind: ReviewDecisionKind;
  reviewerId: string;
  reviewerRole: string;
  comment: string | null;
  decidedAtUtc: string;
}

export interface AnalysisVersion {
  version: number;
  status: ArtifactStatus;
  source: 'Ai' | 'Human';
  model: string;
  promptVersion: string;
  /** The reviewer comment that asked for this version. */
  feedback: string | null;
  basedOnVersion: number | null;
  createdAtUtc: string;
  content: AnalysisContent;
  notes: AnalysisNotes;
  decisions: ReviewDecision[];
}

export interface AnalysisOverview {
  canGenerate: boolean;
  blockedReason: string | null;
  /** Newest first. */
  versions: AnalysisVersion[];
}
