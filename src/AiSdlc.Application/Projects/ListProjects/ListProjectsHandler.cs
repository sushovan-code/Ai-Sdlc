using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using FluentValidation;

namespace AiSdlc.Application.Projects.ListProjects;

/// <summary>Lists only the projects the caller is a member of.</summary>
public sealed class ListProjectsHandler(
    IValidator<ListProjects> validator, ICurrentUser currentUser, IProjectRepository projects)
{
    public async Task<Result<PagedResult<ProjectSummaryDto>>> HandleAsync(
        ListProjects query, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result<PagedResult<ProjectSummaryDto>>.Failure(AuthErrors.InvalidCredentials);
        }

        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<PagedResult<ProjectSummaryDto>>.Failure(validation.ToError());
        }

        var page = await projects.ListForMemberAsync(userId, query.Page, query.PageSize, cancellationToken);
        return Result<PagedResult<ProjectSummaryDto>>.Success(page);
    }
}
