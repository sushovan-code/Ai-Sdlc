namespace AiSdlc.Application.Analysis.Content;

/// <summary>A quote from the uploaded requirements that backs an item, found by chunk reference.</summary>
public sealed record Evidence(string Ref, string Quote);
