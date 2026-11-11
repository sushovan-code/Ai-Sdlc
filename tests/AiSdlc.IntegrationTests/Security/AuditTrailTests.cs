using System.Net.Http.Json;
using AiSdlc.Domain.Auditing;
using AiSdlc.Infrastructure.Persistence;
using AiSdlc.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace AiSdlc.IntegrationTests.Security;

[Collection(SharedApi.Name)]
public sealed class AuditTrailTests(ApiFactory factory)
{
    [Fact]
    public async Task SecurityEvents_AreWrittenToTheAuditTrailWithoutSecrets()
    {
        using var anonymous = factory.CreateClient();
        var user = await anonymous.RegisterNewUserAsync();
        await anonymous.PostAsJsonAsync("/api/v1/auth/login", new { email = user.Email, password = "Wrong#Passw0rd1" });
        await anonymous.LoginAsync(user.Email, TestUsers.Password);
        using var admin = await factory.SignedInAsClientAsync(TestUsers.Admin);
        await admin.PutAsJsonAsync($"/api/v1/users/{user.Id}/role", new { role = "QA" });

        var entries = await AuditEntriesAsync();

        entries.Where(e => e.ActorEmail == user.Email).Select(e => e.Action)
            .ShouldBe([AuditActions.UserRegistered, AuditActions.LoginFailed, AuditActions.LoginSucceeded], ignoreOrder: true);
        entries.ShouldContain(e => e.Action == AuditActions.UserRoleChanged && e.Details == $"{user.Email} -> QA");
        entries.ShouldNotContain(e => (e.Details ?? string.Empty).Contains(TestUsers.Password, StringComparison.Ordinal));
    }

    private async Task<List<AuditEntry>> AuditEntriesAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AiSdlcDbContext>();
        return await db.AuditEntries.AsNoTracking().ToListAsync();
    }
}
