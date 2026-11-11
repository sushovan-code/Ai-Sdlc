using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using AiSdlc.Domain.Auditing;

namespace AiSdlc.Application.Users.SetUserActive;

/// <summary>Deactivates or reactivates an account. Deactivation takes effect on the next request.</summary>
public sealed class SetUserActiveHandler(ICurrentUser currentUser, IUserAccounts accounts, IAuditLog audit)
{
    public async Task<Result<UserDto>> HandleAsync(SetUserActive command, CancellationToken cancellationToken)
    {
        if (command.UserId == currentUser.UserId)
        {
            return Result<UserDto>.Failure(AuthErrors.SelfChange);
        }

        var changed = await accounts.SetActiveAsync(command.UserId, command.IsActive, cancellationToken);
        if (changed.IsFailure)
        {
            return changed;
        }

        var action = command.IsActive ? AuditActions.UserActivated : AuditActions.UserDeactivated;
        await audit.WriteAsync(
            new AuditRecord(action, AuditOutcome.Success, currentUser.UserId, currentUser.Email, changed.Value.Email),
            cancellationToken);
        return changed;
    }
}
