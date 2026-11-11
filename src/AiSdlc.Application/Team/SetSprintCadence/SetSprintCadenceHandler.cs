using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using AiSdlc.Application.Projects;
using AiSdlc.Domain.Auditing;
using AiSdlc.Domain.Planning;

namespace AiSdlc.Application.Team.SetSprintCadence;

/// <summary>Sets how long a sprint is for this project (2, 3 or 4 weeks). Project Managers only.</summary>
public sealed class SetSprintCadenceHandler(ICurrentUser currentUser, IProjectRepository projects, IAuditLog audit)
{
    public async Task<Result<SprintCadenceDto>> HandleAsync(SetSprintCadence command, CancellationToken cancellationToken)
    {
        var opened = await ProjectAccess.OpenAsync(projects, currentUser, command.ProjectId, true, cancellationToken);
        if (opened.IsFailure)
        {
            return Result<SprintCadenceDto>.Failure(opened.Error);
        }

        var project = opened.Value;
        if (!project.SetSprintCadence(command.Weeks))
        {
            return Result<SprintCadenceDto>.Failure(
                Error.Validation("weeks", $"A sprint must be {string.Join(", ", SprintRules.AllowedWeeks)} weeks long."));
        }

        await projects.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync(
            new AuditRecord(
                AuditActions.SprintCadenceChanged,
                AuditOutcome.Success,
                currentUser.UserId,
                currentUser.Email,
                $"{project.Id}: {project.SprintWeeks} weeks"),
            cancellationToken);
        return Result<SprintCadenceDto>.Success(new SprintCadenceDto(project.SprintWeeks));
    }
}
