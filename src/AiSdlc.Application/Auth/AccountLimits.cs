namespace AiSdlc.Application.Auth;

/// <summary>Input limits for account fields, shared by validators and the identity configuration.</summary>
public static class AccountLimits
{
    public const int MaxEmailLength = 254;
    public const int MaxDisplayNameLength = 100;
    public const int MinPasswordLength = 12;
    public const int MaxPasswordLength = 128;
}
