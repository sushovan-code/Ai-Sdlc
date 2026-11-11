using AiSdlc.Domain.Security;

namespace AiSdlc.Domain.Projects;

/// <summary>Which platform roles may act on projects. Project membership is checked separately.</summary>
public static class ProjectPermissions
{
    /// <summary>Create projects, upload and delete files, and manage the member list.</summary>
    public static bool CanManage(string? role) => role == AppRoles.ProjectManager;
}
