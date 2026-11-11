namespace AiSdlc.Domain.Security;

/// <summary>The five roles of the platform. A user holds exactly one role.</summary>
public static class AppRoles
{
    public const string ProjectManager = "ProjectManager";
    public const string Architect = "Architect";
    public const string Developer = "Developer";
    public const string QA = "QA";
    public const string Admin = "Admin";

    /// <summary>Least-privilege role given to self-registered accounts; an administrator promotes them.</summary>
    public const string DefaultSelfRegistrationRole = Developer;

    public static IReadOnlyList<string> All { get; } = [ProjectManager, Architect, Developer, QA, Admin];

    public static bool IsValid(string? role) => role is not null && All.Contains(role);
}
