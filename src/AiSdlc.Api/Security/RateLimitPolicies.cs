namespace AiSdlc.Api.Security;

public static class RateLimitPolicies
{
    public const string Auth = "auth";

    /// <summary>Create, upload and delete calls, limited per signed-in user.</summary>
    public const string Writes = "writes";
}
