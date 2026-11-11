using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using AiSdlc.Domain.Auditing;
using AiSdlc.Domain.Projects;
using FluentValidation;

namespace AiSdlc.Application.Projects.CreateProject;

/// <summary>Creates a project owned by the calling Project Manager, who becomes its first member.</summary>
public sealed class CreateProjectHandler(
    IValidator<CreateProject> validator,
    ICurrentUser currentUser,
    IProjectRepository projects,
    IAuditLog audit,
    TimeProvider time)
{
    public async Task<Result<ProjectSummaryDto>> HandleAsync(CreateProject command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result<ProjectSummaryDto>.Failure(AuthErrors.InvalidCredentials);
        }

        if (!ProjectPermissions.CanManage(currentUser.Role))
        {
            return Result<ProjectSummaryDto>.Failure(ProjectErrors.ManagerRequired);
        }

        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<ProjectSummaryDto>.Failure(validation.ToError());
        }

        var description = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim();
        var project = Project.Create(command.Name.Trim(), description, userId, time.GetUtcNow());
        await projects.AddAsync(project, cancellationToken);
        await projects.SaveChangesAsync(cancellationToken);

        await audit.WriteAsync(
            new AuditRecord(AuditActions.ProjectCreated, AuditOutcome.Success, userId, currentUser.Email, project.Id.ToString()),
            cancellationToken);

        return Result<ProjectSummaryDto>.Success(new ProjectSummaryDto(
            project.Id,
            project.Name,
            project.Description,
            project.Stage,
            project.CreatedAtUtc,
            project.Members.Count,
            HasRequirements: false,
            HasTeamTemplate: false));
    }
}
