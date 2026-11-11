using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using AiSdlc.Domain.Projects;

namespace AiSdlc.Application.Projects;

/// <summary>The resource check every project use case runs first: the caller must be a member, and for changes a Project Manager.</summary>
internal static class ProjectAccess
{
    /// <summary>Non-members get the same "not found" as a missing project, so ids cannot be probed.</summary>
    public static async Task<Result<Project>> OpenAsync(
        IProjectRepository projects,
        ICurrentUser currentUser,
        Guid projectId,
        bool requireManager,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result<Project>.Failure(AuthErrors.InvalidCredentials);
        }

        var project = await projects.GetAsync(projectId, cancellationToken);
        if (project is null || !project.IsMember(userId))
        {
            return Result<Project>.Failure(ProjectErrors.NotFound);
        }

        return requireManager && !ProjectPermissions.CanManage(currentUser.Role)
            ? Result<Project>.Failure(ProjectErrors.ManagerRequired)
            : Result<Project>.Success(project);
    }
}
