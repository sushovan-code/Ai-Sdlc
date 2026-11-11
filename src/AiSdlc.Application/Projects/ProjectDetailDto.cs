using AiSdlc.Domain.Projects;

namespace AiSdlc.Application.Projects;

public sealed record ProjectDetailDto(
    Guid Id,
    string Name,
    string? Description,
    ProjectStage Stage,
    int SprintWeeks,
    DateTimeOffset CreatedAtUtc,
    Guid CreatedById,
    bool CanManage,
    IReadOnlyList<ProjectMemberDto> Members,
    IReadOnlyList<ProjectFileDto> Files);
