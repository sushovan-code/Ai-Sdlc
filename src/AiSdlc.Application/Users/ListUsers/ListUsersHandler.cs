using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using FluentValidation;

namespace AiSdlc.Application.Users.ListUsers;

public sealed class ListUsersHandler(IValidator<ListUsers> validator, IUserAccounts accounts)
{
    public async Task<Result<PagedResult<UserDto>>> HandleAsync(ListUsers query, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<PagedResult<UserDto>>.Failure(validation.ToError());
        }

        var page = await accounts.ListAsync(query.Page, query.PageSize, cancellationToken);
        return Result<PagedResult<UserDto>>.Success(page);
    }
}
