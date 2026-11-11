using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using AiSdlc.Domain.Auditing;
using FluentValidation;

namespace AiSdlc.Application.Auth.Login;

/// <summary>Signs a user in. Every outcome is audited; unknown, wrong-password and disabled look identical to the caller.</summary>
public sealed class LoginHandler(
    IValidator<Login> validator,
    IUserAccounts accounts,
    ITokenService tokens,
    IAuditLog audit)
{
    public async Task<Result<AuthTokenDto>> HandleAsync(Login command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<AuthTokenDto>.Failure(validation.ToError());
        }

        var verified = await accounts.VerifyCredentialsAsync(command.Email, command.Password, cancellationToken);
        if (verified.IsFailure)
        {
            return await RejectAsync(command.Email, verified.Error, cancellationToken);
        }

        var user = verified.Value;
        var token = tokens.Issue(user);
        await audit.WriteAsync(
            new AuditRecord(AuditActions.LoginSucceeded, AuditOutcome.Success, user.Id, user.Email),
            cancellationToken);

        return Result<AuthTokenDto>.Success(new AuthTokenDto(token.Value, token.ExpiresAtUtc, user));
    }

    private async Task<Result<AuthTokenDto>> RejectAsync(string email, Error cause, CancellationToken cancellationToken)
    {
        var isLockedOut = cause.Code == AuthErrors.LockedOutCode;
        var action = isLockedOut ? AuditActions.UserLockedOut : AuditActions.LoginFailed;
        await audit.WriteAsync(
            new AuditRecord(action, AuditOutcome.Failure, ActorEmail: email, Details: cause.Code),
            cancellationToken);

        // Disabled accounts get the same answer as a wrong password so the response reveals nothing.
        var response = isLockedOut ? AuthErrors.LockedOut : AuthErrors.InvalidCredentials;
        return Result<AuthTokenDto>.Failure(response);
    }
}
