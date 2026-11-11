namespace AiSdlc.Application.Analysis.Content;

/// <summary>A governance finding: sensitive data, a policy concern, or a claim the source does not support.</summary>
public sealed record GuardrailFlag(string Code, string Message);
