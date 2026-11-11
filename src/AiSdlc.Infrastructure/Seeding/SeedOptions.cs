namespace AiSdlc.Infrastructure.Seeding;

/// <summary>Bound to the <c>Seed</c> section. Demo users are for local development only.</summary>
public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public bool DemoUsersEnabled { get; set; }

    public string DemoPassword { get; set; } = string.Empty;
}
