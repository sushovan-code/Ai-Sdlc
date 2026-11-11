using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;

namespace AiSdlc.Application.Auth.GetCurrentUser;

/// <summary>Returns the stored profile of the caller (not just the token claims).</summary>
public sealed class GetCurrentUserHandler(ICurrentUser currentUser, IUserAccounts accounts)
{
    public async Task<Result<UserDto>> HandleAsync(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result<UserDto>.Failure(AuthErrors.InvalidCredentials);
        }

        var user = await accounts.FindAsync(userId, cancellationToken);
        return user is null
            ? Result<UserDto>.Failure(AuthErrors.UserNotFound)
            : Result<UserDto>.Success(user);
    }
}
