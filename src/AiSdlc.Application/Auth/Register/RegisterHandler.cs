using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using AiSdlc.Domain.Auditing;
using AiSdlc.Domain.Security;
using FluentValidation;

namespace AiSdlc.Application.Auth.Register;

/// <summary>Self-registration. The new account always gets the least-privilege role; an admin promotes it.</summary>
public sealed class RegisterHandler(IValidator<Register> validator, IUserAccounts accounts, IAuditLog audit)
{
    public async Task<Result<UserDto>> HandleAsync(Register command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<UserDto>.Failure(validation.ToError());
        }

        var created = await accounts.RegisterAsync(
            command.Email.Trim(),
            command.DisplayName.Trim(),
            command.Password,
            AppRoles.DefaultSelfRegistrationRole,
            cancellationToken);

        if (created.IsFailure)
        {
            return created;
        }

        var user = created.Value;
        await audit.WriteAsync(
            new AuditRecord(AuditActions.UserRegistered, AuditOutcome.Success, user.Id, user.Email, user.Role),
            cancellationToken);
        return created;
    }
}
