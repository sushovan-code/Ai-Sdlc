import { AppRole } from '../../core/auth/roles';
import { WORKFLOW, blockReason, furthestStep, stepsFor } from './workflow';

describe('workflow', () => {
  it('has the five stages in order, each after the first closed by a gate with its approver', () => {
    expect(WORKFLOW.map((s) => s.key)).toEqual([
      'files',
      'analysis',
      'architecture',
      'tasks',
      'tests',
    ]);
    expect(WORKFLOW.map((s) => s.gate?.number ?? null)).toEqual([null, 1, 2, 3, 4]);
    expect(WORKFLOW.map((s) => s.gate?.approver ?? null)).toEqual([
      null,
      AppRole.ProjectManager,
      AppRole.Architect,
      AppRole.ProjectManager,
      AppRole.QA,
    ]);
  });

  it('lets a draft project reach only the first stage', () => {
    expect(furthestStep('Draft')).toBe(0);
  });

  it('lets a project whose analysis is approved reach the architecture stage', () => {
    expect(furthestStep('AnalysisApproved')).toBe(2);
    expect(blockReason('AnalysisApproved', 1)).toBeNull();
    expect(stepsFor('AnalysisApproved').map((s) => s.state)).toEqual([
      'done',
      'done',
      'current',
      'upcoming',
      'upcoming',
    ]);
  });

  it('lets a project with both files reach the analysis stage', () => {
    expect(furthestStep('FilesUploaded')).toBe(1);
  });

  describe('blockReason', () => {
    it('asks for both files before leaving the first stage', () => {
      expect(blockReason('Draft', 0)).toBe(
        'Upload both the requirements and the team template to continue.',
      );
    });

    it('allows moving on from the first stage once both files are in', () => {
      expect(blockReason('FilesUploaded', 0)).toBeNull();
    });

    it('names the gate and who must approve it when the next stage is behind a review', () => {
      expect(blockReason('FilesUploaded', 1)).toBe(
        'Gate 1 (Requirements analysis review) must be approved by the Project Manager first.',
      );
    });

    it('names the architect for the architecture gate', () => {
      expect(blockReason('Draft', 2)).toContain('Gate 2');
      expect(blockReason('Draft', 2)).toContain('Architect');
    });

    it('says the last stage has nothing after it', () => {
      expect(blockReason('FilesUploaded', WORKFLOW.length - 1)).toBe('This is the last stage.');
    });
  });

  describe('stepsFor', () => {
    it('marks the first stage current and everything else locked for a draft', () => {
      const steps = stepsFor('Draft');

      expect(steps.map((s) => s.state)).toEqual([
        'current',
        'upcoming',
        'upcoming',
        'upcoming',
        'upcoming',
      ]);
      expect(steps.map((s) => s.reachable)).toEqual([true, false, false, false, false]);
    });

    it('marks the first stage done and the second current once both files are in', () => {
      const steps = stepsFor('FilesUploaded');

      expect(steps.map((s) => s.state)).toEqual([
        'done',
        'current',
        'upcoming',
        'upcoming',
        'upcoming',
      ]);
      expect(steps.map((s) => s.reachable)).toEqual([true, true, false, false, false]);
    });
  });
});
