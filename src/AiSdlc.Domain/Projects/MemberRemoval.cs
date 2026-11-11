namespace AiSdlc.Domain.Projects;

public enum MemberRemoval
{
    Removed,
    NotAMember,

    /// <summary>The creator cannot be removed, so a project always keeps its owner.</summary>
    IsOwner,
}
