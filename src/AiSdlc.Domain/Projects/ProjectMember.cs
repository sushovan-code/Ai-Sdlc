namespace AiSdlc.Domain.Projects;

/// <summary>Grants a user access to a project. Their platform role decides what they may do inside it.</summary>
public sealed class ProjectMember
{
    public Guid ProjectId { get; init; }

    public Guid UserId { get; init; }

    public DateTimeOffset AddedAtUtc { get; init; }
}
