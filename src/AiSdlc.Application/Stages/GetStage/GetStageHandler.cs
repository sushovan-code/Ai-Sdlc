using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using AiSdlc.Application.Projects;
using AiSdlc.Domain.Artifacts;

namespace AiSdlc.Application.Stages.GetStage;

/// <summary>Any project member can read a stage's versions and review history.</summary>
public sealed class GetStageHandler(ICurrentUser currentUser, IProjectRepository projects)
{
    public async Task<Result<StageOverviewDto>> HandleAsync(GetStage query, CancellationToken cancellationToken)
    {
        var opened = await ProjectAccess.OpenAsync(projects, currentUser, query.ProjectId, false, cancellationToken);
        if (opened.IsFailure)
        {
            return Result<StageOverviewDto>.Failure(opened.Error);
        }

        var project = opened.Value;
        var versions = project.Artifacts
            .Where(a => a.Stage == query.Stage)
            .OrderByDescending(a => a.Version)
            .Select(StageMapper.ToDto)
            .ToList();
        var canDraft = project.CanDraft(query.Stage);

        return Result<StageOverviewDto>.Success(new StageOverviewDto(
            StageRules.ApproverRole(query.Stage),
            canDraft,
            canDraft ? null : StageText.BlockedReason(project, query.Stage),
            versions));
    }
}
