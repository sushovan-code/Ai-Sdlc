using AiSdlc.Application.Analysis.Content;

namespace AiSdlc.Application.Analysis.Agents;

/// <summary>Text with sensitive values masked, and what was found.</summary>
public sealed record RedactedText(string Text, IReadOnlyList<GuardrailFlag> Flags);
