using System.Net;
using System.Net.Http.Json;
using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using AiSdlc.Domain.Security;
using AiSdlc.IntegrationTests.Infrastructure;
using Shouldly;

namespace AiSdlc.IntegrationTests.Users;

[Collection(SharedApi.Name)]
public sealed class UserAdministrationTests(ApiFactory factory)
{
    private const string UsersUrl = "/api/v1/users";

    [Fact]
    public async Task List_AsAdmin_ReturnsPagedUsersIncludingDemoAccounts()
    {
        using var admin = await factory.SignedInAsClientAsync(TestUsers.Admin);

        var page = await admin.GetFromJsonAsync<PagedResult<UserDto>>($"{UsersUrl}?page=1&pageSize=100");

        page!.TotalCount.ShouldBeGreaterThanOrEqualTo(5);
        page.Items.Select(u => u.Email).ShouldContain(TestUsers.Architect);
        page.Items.Single(u => u.Email == TestUsers.Admin).Role.ShouldBe(AppRoles.Admin);
    }

    [Fact]
    public async Task List_PageSizeTooLarge_Returns400()
    {
        using var admin = await factory.SignedInAsClientAsync(TestUsers.Admin);

        var response = await admin.GetAsync($"{UsersUrl}?page=1&pageSize=1000");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetRole_AsAdmin_AppliesToTheUsersExistingTokenImmediately()
    {
        using var anonymous = factory.CreateClient();
        var user = await anonymous.RegisterNewUserAsync();
        using var userClient = await factory.SignedInAsClientAsync(user.Email);
        (await userClient.GetAsync(UsersUrl)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        using var admin = await factory.SignedInAsClientAsync(TestUsers.Admin);
        var promoted = await admin.PutAsJsonAsync($"{UsersUrl}/{user.Id}/role", new { role = AppRoles.Admin });

        promoted.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await promoted.Content.ReadFromJsonAsync<UserDto>())!.Role.ShouldBe(AppRoles.Admin);
        (await userClient.GetAsync(UsersUrl)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SetRole_UnknownRole_Returns400()
    {
        using var anonymous = factory.CreateClient();
        var user = await anonymous.RegisterNewUserAsync();
        using var admin = await factory.SignedInAsClientAsync(TestUsers.Admin);

        var response = await admin.PutAsJsonAsync($"{UsersUrl}/{user.Id}/role", new { role = "Superuser" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetRole_UnknownUser_Returns404()
    {
        using var admin = await factory.SignedInAsClientAsync(TestUsers.Admin);

        var response = await admin.PutAsJsonAsync($"{UsersUrl}/{Guid.NewGuid()}/role", new { role = AppRoles.QA });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetRole_OnYourself_Returns409()
    {
        using var admin = await factory.SignedInAsClientAsync(TestUsers.Admin);
        var me = await admin.GetFromJsonAsync<UserDto>("/api/v1/auth/me");

        var response = await admin.PutAsJsonAsync($"{UsersUrl}/{me!.Id}/role", new { role = AppRoles.Developer });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Deactivate_AsAdmin_RejectsExistingTokenAndNewSignInsUntilReactivated()
    {
        using var anonymous = factory.CreateClient();
        var user = await anonymous.RegisterNewUserAsync();
        using var userClient = await factory.SignedInAsClientAsync(user.Email);
        (await userClient.GetAsync("/api/v1/auth/me")).StatusCode.ShouldBe(HttpStatusCode.OK);

        using var admin = await factory.SignedInAsClientAsync(TestUsers.Admin);
        (await admin.PostAsync($"{UsersUrl}/{user.Id}/deactivate", null)).StatusCode.ShouldBe(HttpStatusCode.OK);

        (await userClient.GetAsync("/api/v1/auth/me")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var signIn = await anonymous.PostAsJsonAsync(
            "/api/v1/auth/login", new { email = user.Email, password = TestUsers.Password });
        signIn.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        (await admin.PostAsync($"{UsersUrl}/{user.Id}/activate", null)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await anonymous.LoginAsync(user.Email, TestUsers.Password)).User.Id.ShouldBe(user.Id);
    }

    [Fact]
    public async Task Deactivate_Yourself_Returns409()
    {
        using var admin = await factory.SignedInAsClientAsync(TestUsers.Admin);
        var me = await admin.GetFromJsonAsync<UserDto>("/api/v1/auth/me");

        var response = await admin.PostAsync($"{UsersUrl}/{me!.Id}/deactivate", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }
}
