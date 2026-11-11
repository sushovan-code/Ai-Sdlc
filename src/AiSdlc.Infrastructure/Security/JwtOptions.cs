using System.ComponentModel.DataAnnotations;

namespace AiSdlc.Infrastructure.Security;

/// <summary>Bound to the <c>Jwt</c> section. The signing key comes from user secrets or the host environment, never from a committed file.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int MinSigningKeyLength = 32;
    public const int EphemeralKeyBytes = 48;

    [Required]
    public string Issuer { get; set; } = "aisdlc-api";

    [Required]
    public string Audience { get; set; } = "aisdlc-web";

    [Range(1, 1440)]
    public int AccessTokenMinutes { get; set; } = 60;

    [MinLength(MinSigningKeyLength)]
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Local development only: generate a random key at startup when none is configured (tokens do not survive a restart).</summary>
    public bool AllowEphemeralKey { get; set; }
}
