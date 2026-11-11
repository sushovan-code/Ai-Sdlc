using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using AiSdlc.Application.Projects;
using AiSdlc.Domain.Projects;

namespace AiSdlc.Application.UserDirectory.SearchUsers;

/// <summary>Lets a Project Manager find active colleagues to add to a project. Returns at most a handful of matches.</summary>
public sealed class SearchUsersHandler(ICurrentUser currentUser, IUserAccounts accounts)
{
    public async Task<Result<IReadOnlyList<UserDto>>> HandleAsync(SearchUsers query, CancellationToken cancellationToken)
    {
        if (!ProjectPermissions.CanManage(currentUser.Role))
        {
            return Result<IReadOnlyList<UserDto>>.Failure(ProjectErrors.ManagerRequired);
        }

        if (query.Query is { Length: > SearchUsers.MaxQueryLength })
        {
            return Result<IReadOnlyList<UserDto>>.Failure(Error.Validation("query", "The search text is too long."));
        }

        var users = await accounts.SearchActiveAsync(query.Query?.Trim(), SearchUsers.ResultLimit, cancellationToken);
        return Result<IReadOnlyList<UserDto>>.Success(users);
    }
}
