using System.Net.Http.Headers;
using System.Net.Http.Json;
using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Auth;

namespace AiSdlc.IntegrationTests.Infrastructure;

public static class ApiClientExtensions
{
    public static async Task<AuthTokenDto> LoginAsync(this HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthTokenDto>())!;
    }

    /// <summary>Creates a fresh client that carries a bearer token for <paramref name="email"/>.</summary>
    public static async Task<HttpClient> SignedInAsClientAsync(this ApiFactory factory, string email, string password = TestUsers.Password)
    {
        var client = factory.CreateClient();
        var token = await client.LoginAsync(email, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        return client;
    }

    /// <summary>Self-registers a new account (role Developer) and returns its email.</summary>
    public static async Task<UserDto> RegisterNewUserAsync(this HttpClient client, string password = TestUsers.Password)
    {
        var email = TestUsers.NewEmail();
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register", new { email, displayName = "Test User", password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserDto>())!;
    }
}
