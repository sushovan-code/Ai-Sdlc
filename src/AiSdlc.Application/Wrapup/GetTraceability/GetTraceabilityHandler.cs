using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Analysis.Agents;
using AiSdlc.Application.Analysis.Content;
using AiSdlc.Application.Common;
using AiSdlc.Application.Projects;
using AiSdlc.Application.Stages;
using AiSdlc.Application.Stages.Content;
using AiSdlc.Domain.Artifacts;
using AiSdlc.Domain.Projects;

namespace AiSdlc.Application.Wrapup.GetTraceability;

/// <summary>
/// For every requirement, lists the architecture items, tasks and test cases that trace back to it, following
/// references through the stages (a test that cites a task counts for the requirements that task serves). Any member can read.
/// </summary>
public sealed class GetTraceabilityHandler(ICurrentUser currentUser, IProjectRepository projects)
{
    private static readonly ArtifactStage[] Downstream = [ArtifactStage.Architecture, ArtifactStage.Tasks, ArtifactStage.TestCases];

    public async Task<Result<TraceabilityDto>> HandleAsync(GetTraceability query, CancellationToken cancellationToken)
    {
        var opened = await ProjectAccess.OpenAsync(projects, currentUser, query.ProjectId, false, cancellationToken);
        if (opened.IsFailure)
        {
            return Result<TraceabilityDto>.Failure(opened.Error);
        }

        var analysis = Current(opened.Value, ArtifactStage.Analysis);
        var content = analysis is null ? null : AnalysisJson.Deserialize<AnalysisContent>(analysis.ContentJson);
        if (content is null)
        {
            return Result<TraceabilityDto>.Success(new TraceabilityDto([]));
        }

        var requirements = content.FunctionalRequirements.Concat(content.NonFunctionalRequirements).ToList();
        var requirementIds = requirements.Select(r => r.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var serves = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var byStage = new Dictionary<ArtifactStage, List<(StageItem Item, HashSet<string> Requirements)>>();

        foreach (var stage in Downstream)
        {
            var version = Current(opened.Value, stage);
            var items = version is null ? [] : StageMapper.ReadContent(version).Sections.SelectMany(s => s.Items);
            byStage[stage] = items.Select(item => (item, Serves(item, requirementIds, serves))).ToList();
        }

        var rows = requirements.Select(r => new TraceRowDto(
            r.Id,
            r.Title,
            Links(byStage[ArtifactStage.Architecture], r.Id),
            Links(byStage[ArtifactStage.Tasks], r.Id),
            Links(byStage[ArtifactStage.TestCases], r.Id))).ToList();
        return Result<TraceabilityDto>.Success(new TraceabilityDto(rows));
    }

    /// <summary>The newest version still in play: waiting for review or approved.</summary>
    private static ArtifactVersion? Current(Project project, ArtifactStage stage) =>
        project.Artifacts
            .Where(a => a.Stage == stage && a.Status is ArtifactStatus.PendingReview or ArtifactStatus.Approved)
            .OrderByDescending(a => a.Version)
            .FirstOrDefault();

    private static HashSet<string> Serves(StageItem item, HashSet<string> requirementIds, Dictionary<string, HashSet<string>> serves)
    {
        var covered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var reference in item.Refs)
        {
            if (requirementIds.Contains(reference))
            {
                covered.Add(reference);
            }
            else if (serves.TryGetValue(reference, out var inherited))
            {
                covered.UnionWith(inherited);
            }
        }

        serves[item.Id] = covered;
        return covered;
    }

    private static List<TraceLinkDto> Links(List<(StageItem Item, HashSet<string> Requirements)> items, string requirementId) =>
        items.Where(x => x.Requirements.Contains(requirementId)).Select(x => new TraceLinkDto(x.Item.Id, x.Item.Title)).ToList();
}
