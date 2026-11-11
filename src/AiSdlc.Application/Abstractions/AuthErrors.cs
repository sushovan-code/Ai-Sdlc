using AiSdlc.Application.Common;

namespace AiSdlc.Application.Abstractions;

/// <summary>Error codes shared by the account port and the use cases that call it.</summary>
public static class AuthErrors
{
    public const string InvalidCredentialsCode = "auth.invalid-credentials";
    public const string LockedOutCode = "auth.locked-out";
    public const string AccountDisabledCode = "auth.account-disabled";
    public const string EmailTakenCode = "auth.email-taken";
    public const string UserNotFoundCode = "user.not-found";
    public const string SelfChangeCode = "user.self-change";

    public static Error InvalidCredentials { get; } =
        new(InvalidCredentialsCode, "Invalid email or password.", ErrorType.Unauthorized);

    public static Error LockedOut { get; } =
        new(LockedOutCode, "Too many failed attempts. Try again later.", ErrorType.Unauthorized);

    public static Error UserNotFound { get; } =
        new(UserNotFoundCode, "User not found.", ErrorType.NotFound);

    public static Error SelfChange { get; } =
        new(SelfChangeCode, "You cannot change your own role or deactivate your own account.", ErrorType.Conflict);
}
