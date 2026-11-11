using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using AiSdlc.Domain.Auditing;
using FluentValidation;

namespace AiSdlc.Application.Auth.ChangePassword;

public sealed class ChangePasswordHandler(
    IValidator<ChangePassword> validator,
    ICurrentUser currentUser,
    IUserAccounts accounts,
    IAuditLog audit)
{
    public async Task<Result> HandleAsync(ChangePassword command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result.Failure(AuthErrors.InvalidCredentials);
        }

        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure(validation.ToError());
        }

        var changed = await accounts.ChangePasswordAsync(
            userId, command.CurrentPassword, command.NewPassword, cancellationToken);

        var outcome = changed.IsSuccess ? AuditOutcome.Success : AuditOutcome.Failure;
        await audit.WriteAsync(
            new AuditRecord(AuditActions.PasswordChanged, outcome, userId, currentUser.Email),
            cancellationToken);
        return changed;
    }
}
