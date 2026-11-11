namespace AiSdlc.IntegrationTests.Infrastructure;

/// <summary>The seeded demo accounts and a password that satisfies the policy.</summary>
public static class TestUsers
{
    public const string Password = "Demo#Passw0rd!";
    public const string NewPassword = "Another#Passw0rd9";

    public const string ProjectManager = "pm@demo.local";
    public const string Architect = "architect@demo.local";
    public const string Developer = "dev@demo.local";
    public const string QA = "qa@demo.local";
    public const string Admin = "admin@demo.local";

    public static IEnumerable<object[]> NonAdministrators() =>
    [
        [ProjectManager],
        [Architect],
        [Developer],
        [QA],
    ];

    public static string NewEmail() => $"user-{Guid.NewGuid():N}@test.local";
}
