using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using AiSdlc.Domain.Auditing;
using AiSdlc.Domain.Projects;

namespace AiSdlc.Application.Projects.RemoveProjectMember;

public sealed class RemoveProjectMemberHandler(
    ICurrentUser currentUser, IProjectRepository projects, IAuditLog audit)
{
    public async Task<Result> HandleAsync(RemoveProjectMember command, CancellationToken cancellationToken)
    {
        var opened = await ProjectAccess.OpenAsync(projects, currentUser, command.ProjectId, true, cancellationToken);
        if (opened.IsFailure)
        {
            return Result.Failure(opened.Error);
        }

        var project = opened.Value;
        switch (project.RemoveMember(command.UserId))
        {
            case MemberRemoval.IsOwner:
                return Result.Failure(ProjectErrors.OwnerCannotBeRemoved);
            case MemberRemoval.NotAMember:
                return Result.Failure(ProjectErrors.NotMember);
        }

        await projects.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync(
            new AuditRecord(
                AuditActions.ProjectMemberRemoved,
                AuditOutcome.Success,
                currentUser.UserId,
                currentUser.Email,
                $"{project.Id}: {command.UserId}"),
            cancellationToken);
        return Result.Success();
    }
}
