using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using AiSdlc.Domain.Auditing;
using FluentValidation;

namespace AiSdlc.Application.Users.SetUserRole;

public sealed class SetUserRoleHandler(
    IValidator<SetUserRole> validator,
    ICurrentUser currentUser,
    IUserAccounts accounts,
    IAuditLog audit)
{
    public async Task<Result<UserDto>> HandleAsync(SetUserRole command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<UserDto>.Failure(validation.ToError());
        }

        // An administrator cannot demote themselves, which would risk leaving no administrator.
        if (command.UserId == currentUser.UserId)
        {
            return Result<UserDto>.Failure(AuthErrors.SelfChange);
        }

        var changed = await accounts.SetRoleAsync(command.UserId, command.Role, cancellationToken);
        if (changed.IsFailure)
        {
            return changed;
        }

        await audit.WriteAsync(
            new AuditRecord(
                AuditActions.UserRoleChanged,
                AuditOutcome.Success,
                currentUser.UserId,
                currentUser.Email,
                $"{changed.Value.Email} -> {command.Role}"),
            cancellationToken);
        return changed;
    }
}
