using AiSdlc.Application.Common;

namespace AiSdlc.Application.Abstractions;

/// <summary>Account storage and credential checks. The adapter hides the identity framework.</summary>
public interface IUserAccounts
{
    Task<Result<UserDto>> RegisterAsync(
        string email, string displayName, string password, string role, CancellationToken cancellationToken);

    /// <summary>Checks the password and applies lockout. Fails with <see cref="AuthErrors"/> codes.</summary>
    Task<Result<UserDto>> VerifyCredentialsAsync(string email, string password, CancellationToken cancellationToken);

    Task<Result> ChangePasswordAsync(
        Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken);

    Task<UserDto?> FindAsync(Guid userId, CancellationToken cancellationToken);

    Task<PagedResult<UserDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<Result<UserDto>> SetRoleAsync(Guid userId, string role, CancellationToken cancellationToken);

    Task<Result<UserDto>> SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken);
}
