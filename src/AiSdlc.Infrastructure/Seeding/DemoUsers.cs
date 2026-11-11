using AiSdlc.Domain.Security;

namespace AiSdlc.Infrastructure.Seeding;

/// <summary>The local demo accounts, one per role.</summary>
internal static class DemoUsers
{
    public sealed record Entry(string Email, string DisplayName, string Role);

    public static IReadOnlyList<Entry> All { get; } =
    [
        new("pm@demo.local", "Demo Project Manager", AppRoles.ProjectManager),
        new("architect@demo.local", "Demo Architect", AppRoles.Architect),
        new("dev@demo.local", "Demo Developer", AppRoles.Developer),
        new("qa@demo.local", "Demo QA", AppRoles.QA),
        new("admin@demo.local", "Demo Administrator", AppRoles.Admin),
    ];
}
