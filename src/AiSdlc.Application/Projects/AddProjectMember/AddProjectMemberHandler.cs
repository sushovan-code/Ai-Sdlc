using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using AiSdlc.Domain.Auditing;

namespace AiSdlc.Application.Projects.AddProjectMember;

public sealed class AddProjectMemberHandler(
    ICurrentUser currentUser,
    IProjectRepository projects,
    IUserAccounts accounts,
    IAuditLog audit,
    TimeProvider time)
{
    public async Task<Result<ProjectMemberDto>> HandleAsync(AddProjectMember command, CancellationToken cancellationToken)
    {
        var opened = await ProjectAccess.OpenAsync(projects, currentUser, command.ProjectId, true, cancellationToken);
        if (opened.IsFailure)
        {
            return Result<ProjectMemberDto>.Failure(opened.Error);
        }

        var project = opened.Value;
        var user = await accounts.FindAsync(command.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Result<ProjectMemberDto>.Failure(ProjectErrors.UserNotFound);
        }

        if (!project.AddMember(user.Id, time.GetUtcNow()))
        {
            return Result<ProjectMemberDto>.Failure(ProjectErrors.AlreadyMember);
        }

        await projects.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync(
            new AuditRecord(
                AuditActions.ProjectMemberAdded,
                AuditOutcome.Success,
                currentUser.UserId,
                currentUser.Email,
                $"{project.Id}: {user.Email}"),
            cancellationToken);

        return Result<ProjectMemberDto>.Success(
            new ProjectMemberDto(user.Id, user.DisplayName, user.Email, user.Role, IsOwner: false));
    }
}
