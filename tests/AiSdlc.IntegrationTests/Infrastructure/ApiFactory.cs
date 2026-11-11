using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace AiSdlc.IntegrationTests.Infrastructure;

/// <summary>Hosts the real API against a throw-away SQLite file with the demo users seeded.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private const string DevelopmentEnvironment = "Development";
    private const string GenerousRateLimit = "10000";

    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"aisdlc-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(DevelopmentEnvironment);

        // UseSetting is visible to Program.cs while it composes services, unlike late configuration sources.
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_databasePath}");
        builder.UseSetting("Seed:DemoUsersEnabled", "true");
        builder.UseSetting("Seed:DemoPassword", TestUsers.Password);
        builder.UseSetting("Jwt:AllowEphemeralKey", "true");
        builder.UseSetting("RateLimiting:AuthPermitLimit", GenerousRateLimit);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
        {
            return;
        }

        SqliteConnection.ClearAllPools();
        File.Delete(_databasePath);
    }
}
