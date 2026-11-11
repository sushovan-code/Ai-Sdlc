using AiSdlc.Application.Common;

namespace AiSdlc.Application.Projects;

public static class ProjectErrors
{
    public const string NotFoundCode = "project.not-found";
    public const string ManagerRequiredCode = "project.manager-required";
    public const string AlreadyMemberCode = "project.already-member";
    public const string NotMemberCode = "project.not-member";
    public const string OwnerCannotBeRemovedCode = "project.owner-cannot-be-removed";
    public const string UserNotFoundCode = "project.user-not-found";
    public const string FileNotFoundCode = "project.file-not-found";

    /// <summary>Also returned to users who are not members, so project ids cannot be probed.</summary>
    public static Error NotFound { get; } = new(NotFoundCode, "Project not found.", ErrorType.NotFound);

    public static Error ManagerRequired { get; } =
        new(ManagerRequiredCode, "Only a Project Manager can do this.", ErrorType.Forbidden);

    /// <summary>For the stages reviewed by someone other than the Project Manager.</summary>
    public static Error RoleRequired(string roleLabel) =>
        new(ManagerRequiredCode, $"Only the {roleLabel} can do this.", ErrorType.Forbidden);

    public const string AuditRestrictedCode = "project.audit-restricted";

    public static Error AuditRestricted { get; } = new(
        AuditRestrictedCode,
        "The audit trail is for the Project Manager, Architect and QA.",
        ErrorType.Forbidden);

    public static Error AlreadyMember { get; } =
        new(AlreadyMemberCode, "That user is already on the project.", ErrorType.Conflict);

    public static Error NotMember { get; } =
        new(NotMemberCode, "That user is not on the project.", ErrorType.NotFound);

    public static Error OwnerCannotBeRemoved { get; } =
        new(OwnerCannotBeRemovedCode, "The project owner cannot be removed.", ErrorType.Conflict);

    public static Error UserNotFound { get; } =
        new(UserNotFoundCode, "User not found or deactivated.", ErrorType.NotFound);

    public static Error FileNotFound { get; } =
        new(FileNotFoundCode, "File not found.", ErrorType.NotFound);
}
