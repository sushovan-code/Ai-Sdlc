using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AiSdlc.Application.Abstractions;
using AiSdlc.Domain.Security;
using AiSdlc.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace AiSdlc.IntegrationTests.Auth;

[Collection(SharedApi.Name)]
public sealed class AuthEndpointsTests(ApiFactory factory)
{
    private const string LoginUrl = "/api/v1/auth/login";
    private const string RegisterUrl = "/api/v1/auth/register";
    private const string MeUrl = "/api/v1/auth/me";
    private const string ChangePasswordUrl = "/api/v1/auth/change-password";
    private const int LockoutThreshold = 5;

    private readonly HttpClient _anonymous = factory.CreateClient();

    [Fact]
    public async Task Login_DemoProjectManager_ReturnsTokenAndProfile()
    {
        var token = await _anonymous.LoginAsync(TestUsers.ProjectManager, TestUsers.Password);

        token.AccessToken.ShouldNotBeNullOrWhiteSpace();
        token.User.Email.ShouldBe(TestUsers.ProjectManager);
        token.User.Role.ShouldBe(AppRoles.ProjectManager);
        token.ExpiresAtUtc.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401WithGenericProblem()
    {
        var response = await _anonymous.PostAsJsonAsync(
            LoginUrl, new { email = TestUsers.Developer, password = "Wrong#Passw0rd1" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var problem = (await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>())!;
        problem.Title.ShouldBe("Invalid email or password.");
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsSameResponseAsWrongPassword()
    {
        var unknown = await _anonymous.PostAsJsonAsync(
            LoginUrl, new { email = TestUsers.NewEmail(), password = TestUsers.Password });
        var wrongPassword = await _anonymous.PostAsJsonAsync(
            LoginUrl, new { email = TestUsers.Developer, password = "Wrong#Passw0rd1" });

        unknown.StatusCode.ShouldBe(wrongPassword.StatusCode);
        var unknownProblem = (await unknown.Content.ReadFromJsonAsync<HttpValidationProblemDetails>())!;
        var wrongProblem = (await wrongPassword.Content.ReadFromJsonAsync<HttpValidationProblemDetails>())!;
        unknownProblem.Title.ShouldBe(wrongProblem.Title);
        unknownProblem.Extensions["code"]!.ToString().ShouldBe(wrongProblem.Extensions["code"]!.ToString());
    }

    [Fact]
    public async Task Login_EmptyBody_Returns400()
    {
        var response = await _anonymous.PostAsJsonAsync(LoginUrl, new { email = "", password = "" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_NewUser_CreatesDeveloperWhoCanSignIn()
    {
        var created = await _anonymous.RegisterNewUserAsync();

        created.Role.ShouldBe(AppRoles.Developer);
        created.IsActive.ShouldBeTrue();
        var token = await _anonymous.LoginAsync(created.Email, TestUsers.Password);
        token.User.Id.ShouldBe(created.Id);
    }

    [Fact]
    public async Task Register_AttemptToChooseRole_IsIgnoredAndGivesDeveloper()
    {
        var email = TestUsers.NewEmail();

        var response = await _anonymous.PostAsJsonAsync(
            RegisterUrl, new { email, displayName = "Sneaky", password = TestUsers.Password, role = AppRoles.Admin });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var user = (await response.Content.ReadFromJsonAsync<UserDto>())!;
        user.Role.ShouldBe(AppRoles.Developer);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var existing = await _anonymous.RegisterNewUserAsync();

        var response = await _anonymous.PostAsJsonAsync(
            RegisterUrl, new { email = existing.Email, displayName = "Again", password = TestUsers.Password });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WeakPassword_Returns400WithPasswordErrors()
    {
        var response = await _anonymous.PostAsJsonAsync(
            RegisterUrl, new { email = TestUsers.NewEmail(), displayName = "Weak", password = "alllowercase12" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = (await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>())!;
        problem.Errors.ShouldContainKey("password");
    }

    [Fact]
    public async Task Login_AfterFiveWrongPasswords_LocksAccountEvenForCorrectPassword()
    {
        var user = await _anonymous.RegisterNewUserAsync();

        for (var attempt = 0; attempt < LockoutThreshold; attempt++)
        {
            var failed = await _anonymous.PostAsJsonAsync(
                LoginUrl, new { email = user.Email, password = "Wrong#Passw0rd1" });
            failed.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        var locked = await _anonymous.PostAsJsonAsync(LoginUrl, new { email = user.Email, password = TestUsers.Password });

        locked.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var problem = (await locked.Content.ReadFromJsonAsync<HttpValidationProblemDetails>())!;
        problem.Extensions["code"]!.ToString().ShouldBe(AuthErrors.LockedOutCode);
    }

    [Fact]
    public async Task Me_WithValidToken_ReturnsStoredProfile()
    {
        using var client = await factory.SignedInAsClientAsync(TestUsers.QA);

        var user = await client.GetFromJsonAsync<UserDto>(MeUrl);

        user!.Email.ShouldBe(TestUsers.QA);
        user.Role.ShouldBe(AppRoles.QA);
    }

    [Fact]
    public async Task Me_WithTamperedToken_Returns401()
    {
        var token = await _anonymous.LoginAsync(TestUsers.QA, TestUsers.Password);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token.AccessToken[..^3] + "abc");

        var response = await client.GetAsync(MeUrl);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_WithCorrectCurrent_AllowsSignInWithNewPasswordOnly()
    {
        var user = await _anonymous.RegisterNewUserAsync();
        using var client = await factory.SignedInAsClientAsync(user.Email);

        var response = await client.PostAsJsonAsync(
            ChangePasswordUrl, new { currentPassword = TestUsers.Password, newPassword = TestUsers.NewPassword });

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await _anonymous.LoginAsync(user.Email, TestUsers.NewPassword)).User.Id.ShouldBe(user.Id);
        var oldPassword = await _anonymous.PostAsJsonAsync(
            LoginUrl, new { email = user.Email, password = TestUsers.Password });
        oldPassword.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrent_Returns400()
    {
        var user = await _anonymous.RegisterNewUserAsync();
        using var client = await factory.SignedInAsClientAsync(user.Email);

        var response = await client.PostAsJsonAsync(
            ChangePasswordUrl, new { currentPassword = "Wrong#Passw0rd1", newPassword = TestUsers.NewPassword });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = (await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>())!;
        problem.Errors.ShouldContainKey("currentPassword");
    }
}
