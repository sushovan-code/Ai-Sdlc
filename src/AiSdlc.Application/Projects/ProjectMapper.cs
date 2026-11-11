using AiSdlc.Application.Abstractions;
using AiSdlc.Domain.Projects;

namespace AiSdlc.Application.Projects;

internal static class ProjectMapper
{
    private const string UnknownUserName = "Unknown user";

    /// <summary>Builds the detail view; member and uploader names are looked up in one query.</summary>
    public static async Task<ProjectDetailDto> ToDetailAsync(
        Project project, ICurrentUser currentUser, IUserAccounts accounts, CancellationToken cancellationToken)
    {
        var visibleFiles = project.Files.Where(f => !f.IsDeleted).OrderBy(f => f.Kind).ThenByDescending(f => f.Version).ToList();
        var userIds = project.Members.Select(m => m.UserId).Concat(visibleFiles.Select(f => f.UploadedById)).ToHashSet();
        var users = (await accounts.FindManyAsync(userIds, cancellationToken)).ToDictionary(u => u.Id);

        var members = project.Members
            .Where(m => users.ContainsKey(m.UserId))
            .Select(m => users[m.UserId])
            .Select(u => new ProjectMemberDto(u.Id, u.DisplayName, u.Email, u.Role, u.Id == project.CreatedById))
            .OrderByDescending(m => m.IsOwner).ThenBy(m => m.DisplayName)
            .ToList();

        var files = visibleFiles
            .Select(f => new ProjectFileDto(
                f.Id,
                f.Kind,
                f.Version,
                f.FileName,
                f.SizeBytes,
                f.Sha256,
                f.UploadedAtUtc,
                f.UploadedById,
                users.TryGetValue(f.UploadedById, out var uploader) ? uploader.DisplayName : UnknownUserName,
                project.LatestFile(f.Kind)?.Id == f.Id))
            .ToList();

        return new ProjectDetailDto(
            project.Id,
            project.Name,
            project.Description,
            project.Stage,
            project.SprintWeeks,
            project.CreatedAtUtc,
            project.CreatedById,
            ProjectPermissions.CanManage(currentUser.Role),
            members,
            files);
    }
}
