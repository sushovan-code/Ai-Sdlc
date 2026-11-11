using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;

namespace AiSdlc.Application.Projects.GetProject;

public sealed class GetProjectHandler(ICurrentUser currentUser, IProjectRepository projects, IUserAccounts accounts)
{
    public async Task<Result<ProjectDetailDto>> HandleAsync(GetProject query, CancellationToken cancellationToken)
    {
        var opened = await ProjectAccess.OpenAsync(projects, currentUser, query.ProjectId, false, cancellationToken);
        if (opened.IsFailure)
        {
            return Result<ProjectDetailDto>.Failure(opened.Error);
        }

        var detail = await ProjectMapper.ToDetailAsync(opened.Value, currentUser, accounts, cancellationToken);
        return Result<ProjectDetailDto>.Success(detail);
    }
}
