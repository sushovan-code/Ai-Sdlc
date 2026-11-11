namespace AiSdlc.Application.Analysis.Content;

/// <summary>A reviewer-agent observation about gaps, inconsistencies or risks in the draft.</summary>
public sealed record CriticNote(string Severity, string Note);
