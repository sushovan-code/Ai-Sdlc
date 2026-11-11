namespace AiSdlc.Application.Analysis.Content;

/// <summary>A numbered piece of the uploaded document. The model cites chunks by id so claims can be checked.</summary>
public sealed record SourceChunk(string Id, string Text);
