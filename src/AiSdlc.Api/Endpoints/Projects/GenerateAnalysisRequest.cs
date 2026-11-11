namespace AiSdlc.Api.Endpoints.Projects;

/// <summary>Optional reviewer feedback that steers a regeneration.</summary>
public sealed record GenerateAnalysisRequest(string? Feedback);
