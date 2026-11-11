using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Analysis.GenerateAnalysis;
using AiSdlc.Application.Common;
using AiSdlc.Application.Projects;
using AiSdlc.Domain.Artifacts;

namespace AiSdlc.Application.Analysis.GetAnalysis;

/// <summary>Any project member can read the analysis versions and their review history.</summary>
public sealed class GetAnalysisHandler(ICurrentUser currentUser, IProjectRepository projects)
{
    public async Task<Result<AnalysisOverviewDto>> HandleAsync(GetAnalysis query, CancellationToken cancellationToken)
    {
        var opened = await ProjectAccess.OpenAsync(projects, currentUser, query.ProjectId, false, cancellationToken);
        if (opened.IsFailure)
        {
            return Result<AnalysisOverviewDto>.Failure(opened.Error);
        }

        var project = opened.Value;
        var versions = project.Artifacts
            .Where(a => a.Stage == ArtifactStage.Analysis)
            .OrderByDescending(a => a.Version)
            .Select(AnalysisMapper.ToDto)
            .ToList();

        return Result<AnalysisOverviewDto>.Success(new AnalysisOverviewDto(
            project.CanDraftAnalysis,
            project.CanDraftAnalysis ? null : GenerateAnalysisHandler.BlockedReason(project),
            versions));
    }
}
