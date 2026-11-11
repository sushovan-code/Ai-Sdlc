using AiSdlc.Domain.Projects;

namespace AiSdlc.Application.Stages.Agents;

/// <summary>What a stage is drafted from: earlier approved output as prompt text, the ids it may cite, and the team.</summary>
public sealed record StageSource(
    string Text,
    IReadOnlySet<string> KnownRefs,
    IReadOnlyList<TeamMember> Team,
    int SprintWeeks);
