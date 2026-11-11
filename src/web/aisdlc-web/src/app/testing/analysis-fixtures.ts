import type {
  AnalysisContent,
  AnalysisOverview,
  AnalysisVersion,
} from '../core/api/analysis-types';

export function makeContent(overrides: Partial<AnalysisContent> = {}): AnalysisContent {
  return {
    summary: 'Customers pay with a saved card.',
    functionalRequirements: [
      {
        id: 'FR-1',
        title: 'Pay with a saved card',
        description: 'A customer can pay with a stored card.',
        priority: 'High',
        sourceRefs: ['C1'],
      },
    ],
    nonFunctionalRequirements: [],
    ambiguities: [{ id: 'AMB-1', text: 'Which cards are accepted?', sourceRefs: ['C1'] }],
    gaps: [],
    assumptions: ['Cards are tokenised.'],
    risks: [],
    evidence: [{ ref: 'C1', quote: 'Customers can pay with a saved card.' }],
    ...overrides,
  };
}

export function makeVersion(overrides: Partial<AnalysisVersion> = {}): AnalysisVersion {
  return {
    version: 1,
    status: 'PendingReview',
    source: 'Ai',
    model: 'fake-analysis-1',
    promptVersion: 'analysis-v1',
    feedback: null,
    basedOnVersion: null,
    createdAtUtc: '2026-11-11T10:00:00Z',
    content: makeContent(),
    notes: { critic: [], flags: [] },
    decisions: [],
    ...overrides,
  };
}

export function makeOverview(overrides: Partial<AnalysisOverview> = {}): AnalysisOverview {
  return { canGenerate: true, blockedReason: null, versions: [], ...overrides };
}
