using AiSdlc.Application.Analysis.Content;

namespace AiSdlc.Application.Stages.Content;

/// <summary>Guardrail flags for a draft, and (for tasks) the sprint assignments the scheduler made.</summary>
public sealed record StageNotes(
    IReadOnlyList<CriticNote> Critic,
    IReadOnlyList<GuardrailFlag> Flags,
    IReadOnlyList<PlannedAssignment> Assignments);
