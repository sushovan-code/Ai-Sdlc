using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using AiSdlc.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AiSdlc.Infrastructure.Identity;

/// <summary>ASP.NET Core Identity adapter for <see cref="IUserAccounts"/>.</summary>
internal sealed class IdentityUserAccounts(
    UserManager<ApplicationUser> users,
    AiSdlcDbContext db,
    TimeProvider time) : IUserAccounts
{
    private const string PasswordField = "password";
    private const string CurrentPasswordField = "currentPassword";
    private const string LikeEscape = "\\";

    public async Task<Result<UserDto>> RegisterAsync(
        string email, string displayName, string password, string role, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName,
            IsActive = true,
            CreatedAtUtc = time.GetUtcNow(),
        };

        var created = await users.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            return Result<UserDto>.Failure(MapRegistrationFailure(created));
        }

        var roleAdded = await users.AddToRoleAsync(user, role);
        if (!roleAdded.Succeeded)
        {
            // Do not leave a role-less account behind.
            await users.DeleteAsync(user);
            return Result<UserDto>.Failure(MapRegistrationFailure(roleAdded));
        }

        return Result<UserDto>.Success(ToDto(user, role));
    }

    public async Task<Result<UserDto>> VerifyCredentialsAsync(
        string email, string password, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            return Result<UserDto>.Failure(AuthErrors.InvalidCredentials);
        }

        if (await users.IsLockedOutAsync(user))
        {
            return Result<UserDto>.Failure(AuthErrors.LockedOut);
        }

        if (!await users.CheckPasswordAsync(user, password))
        {
            await users.AccessFailedAsync(user);
            var lockedNow = await users.IsLockedOutAsync(user);
            return Result<UserDto>.Failure(lockedNow ? AuthErrors.LockedOut : AuthErrors.InvalidCredentials);
        }

        if (!user.IsActive)
        {
            return Result<UserDto>.Failure(
                new Error(AuthErrors.AccountDisabledCode, "Account is disabled.", ErrorType.Unauthorized));
        }

        await users.ResetAccessFailedCountAsync(user);
        return Result<UserDto>.Success(await ToDtoAsync(user));
    }

    public async Task<Result> ChangePasswordAsync(
        Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure(AuthErrors.UserNotFound);
        }

        var changed = await users.ChangePasswordAsync(user, currentPassword, newPassword);
        return changed.Succeeded ? Result.Success() : Result.Failure(MapPasswordChangeFailure(changed));
    }

    public async Task<UserDto?> FindAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        return user is null ? null : await ToDtoAsync(user);
    }

    public async Task<IReadOnlyList<UserDto>> FindManyAsync(
        IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return [];
        }

        return await UserRows().Where(r => userIds.Contains(r.User.Id)).Select(ToView).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserDto>> SearchActiveAsync(
        string? query, int limit, CancellationToken cancellationToken)
    {
        var rows = UserRows().Where(r => r.User.IsActive);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var pattern = $"%{EscapeLike(query)}%";
            rows = rows.Where(r =>
                EF.Functions.Like(r.User.DisplayName, pattern, LikeEscape)
                || EF.Functions.Like(r.User.Email!, pattern, LikeEscape));
        }

        return await rows.OrderBy(r => r.User.DisplayName).Take(limit).Select(ToView).ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<UserDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var total = await db.Users.CountAsync(cancellationToken);

        var items = await UserRows()
            .OrderBy(r => r.User.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToView)
            .ToListAsync(cancellationToken);

        return new PagedResult<UserDto>(items, page, pageSize, total);
    }

    public async Task<Result<UserDto>> SetRoleAsync(Guid userId, string role, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result<UserDto>.Failure(AuthErrors.UserNotFound);
        }

        var current = await users.GetRolesAsync(user);
        if (!current.Contains(role))
        {
            await users.RemoveFromRolesAsync(user, current);
            await users.AddToRoleAsync(user, role);
            await users.UpdateSecurityStampAsync(user);
        }

        return Result<UserDto>.Success(ToDto(user, role));
    }

    public async Task<Result<UserDto>> SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result<UserDto>.Failure(AuthErrors.UserNotFound);
        }

        user.IsActive = isActive;
        await users.UpdateAsync(user);
        await users.UpdateSecurityStampAsync(user);
        return Result<UserDto>.Success(await ToDtoAsync(user));
    }

    /// <summary>The DTO projection. Apply it last: filtering and ordering must happen on the entity columns, not on the DTO.</summary>
    private static readonly System.Linq.Expressions.Expression<Func<UserRow, UserDto>> ToView =
        row => new UserDto(row.User.Id, row.User.Email!, row.User.DisplayName, row.Role, row.User.IsActive);

    /// <summary>Users joined to their single role. Filter and order on this, then project with <see cref="ToView"/>.</summary>
    private IQueryable<UserRow> UserRows() =>
        from user in db.Users.AsNoTracking()
        join userRole in db.UserRoles on user.Id equals userRole.UserId
        join role in db.Roles on userRole.RoleId equals role.Id
        select new UserRow { User = user, Role = role.Name! };

    private sealed class UserRow
    {
        public required ApplicationUser User { get; init; }

        public required string Role { get; init; }
    }

    private static string EscapeLike(string text) =>
        text.Replace(LikeEscape, LikeEscape + LikeEscape, StringComparison.Ordinal)
            .Replace("%", LikeEscape + "%", StringComparison.Ordinal)
            .Replace("_", LikeEscape + "_", StringComparison.Ordinal);

    private static UserDto ToDto(ApplicationUser user, string role) =>
        new(user.Id, user.Email!, user.DisplayName, role, user.IsActive);

    private async Task<UserDto> ToDtoAsync(ApplicationUser user)
    {
        var roles = await users.GetRolesAsync(user);
        return ToDto(user, roles.Single());
    }

    private static Error MapRegistrationFailure(IdentityResult result)
    {
        var duplicate = result.Errors.Any(e =>
            e.Code is nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName));
        if (duplicate)
        {
            return new Error(AuthErrors.EmailTakenCode, "An account with this email already exists.", ErrorType.Conflict);
        }

        return Error.Validation(PasswordField, result.Errors.Select(e => e.Description).ToArray());
    }

    private static Error MapPasswordChangeFailure(IdentityResult result)
    {
        var mismatch = result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.PasswordMismatch));
        return mismatch
            ? Error.Validation(CurrentPasswordField, "The current password is incorrect.")
            : Error.Validation(PasswordField, result.Errors.Select(e => e.Description).ToArray());
    }
}
