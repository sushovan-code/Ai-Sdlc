namespace AiSdlc.Domain.Auditing;

/// <summary>Stable action names written to the audit trail.</summary>
public static class AuditActions
{
    public const string UserRegistered = "user.registered";
    public const string LoginSucceeded = "auth.login-succeeded";
    public const string LoginFailed = "auth.login-failed";
    public const string UserLockedOut = "auth.user-locked-out";
    public const string PasswordChanged = "auth.password-changed";
    public const string UserRoleChanged = "user.role-changed";
    public const string UserDeactivated = "user.deactivated";
    public const string UserActivated = "user.activated";
}
