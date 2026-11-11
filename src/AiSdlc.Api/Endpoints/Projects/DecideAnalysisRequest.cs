namespace AiSdlc.Api.Endpoints.Projects;

/// <summary>The reviewer's comment. Optional when approving, required when rejecting.</summary>
public sealed record DecideAnalysisRequest(string? Comment);
