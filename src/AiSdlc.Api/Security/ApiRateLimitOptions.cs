namespace AiSdlc.Api.Security;

/// <summary>Bound to the <c>RateLimiting</c> section.</summary>
public sealed class ApiRateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public int AuthPermitLimit { get; set; } = 10;

    public int AuthWindowSeconds { get; set; } = 60;

    public int WritesPermitLimit { get; set; } = 60;

    public int WritesWindowSeconds { get; set; } = 60;

    /// <summary>Each generation makes several paid model calls, so it gets a much tighter limit than other writes.</summary>
    public int AiPermitLimit { get; set; } = 5;

    public int AiWindowSeconds { get; set; } = 60;
}
