using AiSdlc.Domain.Projects;

namespace AiSdlc.Application.Projects;

public sealed record ProjectSummaryDto(
    Guid Id,
    string Name,
    string? Description,
    ProjectStage Stage,
    DateTimeOffset CreatedAtUtc,
    int MemberCount,
    bool HasRequirements,
    bool HasTeamTemplate);
