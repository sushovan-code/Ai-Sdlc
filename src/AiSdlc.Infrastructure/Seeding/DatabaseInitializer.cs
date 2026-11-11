using AiSdlc.Domain.Security;
using AiSdlc.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiSdlc.Infrastructure.Seeding;

/// <summary>On start: applies migrations, ensures the roles exist and, when enabled, seeds the demo users.</summary>
internal sealed class DatabaseInitializer(
    IServiceScopeFactory scopeFactory,
    IOptions<SeedOptions> seedOptions,
    TimeProvider time,
    ILogger<DatabaseInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AiSdlcDbContext>();
        await db.Database.MigrateAsync(cancellationToken);

        await EnsureRolesAsync(scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>());

        var seed = seedOptions.Value;
        if (seed.DemoUsersEnabled)
        {
            await SeedDemoUsersAsync(scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(), seed);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task EnsureRolesAsync(RoleManager<IdentityRole<Guid>> roles)
    {
        foreach (var name in AppRoles.All)
        {
            if (!await roles.RoleExistsAsync(name))
            {
                await roles.CreateAsync(new IdentityRole<Guid>(name) { Id = Guid.NewGuid() });
            }
        }
    }

    private async Task SeedDemoUsersAsync(UserManager<ApplicationUser> users, SeedOptions seed)
    {
        foreach (var demo in DemoUsers.All)
        {
            if (await users.FindByEmailAsync(demo.Email) is not null)
            {
                continue;
            }

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = demo.Email,
                Email = demo.Email,
                EmailConfirmed = true,
                DisplayName = demo.DisplayName,
                IsActive = true,
                CreatedAtUtc = time.GetUtcNow(),
            };

            var created = await users.CreateAsync(user, seed.DemoPassword);
            if (!created.Succeeded)
            {
                logger.LogWarning(
                    "Could not seed demo user {Email}: {Errors}",
                    demo.Email,
                    string.Join("; ", created.Errors.Select(e => e.Code)));
                continue;
            }

            await users.AddToRoleAsync(user, demo.Role);
        }
    }
}
