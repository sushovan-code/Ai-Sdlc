namespace AiSdlc.Api.Security;

/// <summary>Names of the authorization policies. Every endpoint declares one (or the fallback applies).</summary>
public static class AuthorizationPolicies
{
    public const string UserAdministration = "user-administration";
}
