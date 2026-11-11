namespace AiSdlc.Infrastructure.Security;

/// <summary>Raw JWT claim names (the bearer handler does not remap them to long URI types).</summary>
public static class JwtClaims
{
    public const string Subject = "sub";
    public const string Email = "email";
    public const string Name = "name";
    public const string Role = "role";
}
