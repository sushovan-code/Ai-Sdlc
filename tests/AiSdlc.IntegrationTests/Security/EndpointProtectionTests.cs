using System.Net;
using System.Net.Http.Json;
using AiSdlc.IntegrationTests.Infrastructure;
using Shouldly;

namespace AiSdlc.IntegrationTests.Security;

/// <summary>Every protected endpoint must reject anonymous callers, and admin-only ones must reject every other role.</summary>
[Collection(SharedApi.Name)]
public sealed class EndpointProtectionTests(ApiFactory factory)
{
    private static readonly Guid AnyUserId = Guid.NewGuid();

    public static IEnumerable<object[]> ProtectedEndpoints() =>
    [
        [HttpMethod.Get.Method, "/api/v1/auth/me"],
        [HttpMethod.Post.Method, "/api/v1/auth/change-password"],
        [HttpMethod.Get.Method, "/api/v1/users"],
        [HttpMethod.Put.Method, $"/api/v1/users/{AnyUserId}/role"],
        [HttpMethod.Post.Method, $"/api/v1/users/{AnyUserId}/deactivate"],
        [HttpMethod.Post.Method, $"/api/v1/users/{AnyUserId}/activate"],
    ];

    public static IEnumerable<object[]> AdminOnlyEndpoints() =>
        ProtectedEndpoints().Where(e => ((string)e[1]).StartsWith("/api/v1/users", StringComparison.Ordinal));

    public static IEnumerable<object[]> EveryRoleAgainstEveryAdminEndpoint() =>
        from role in TestUsers.NonAdministrators()
        from endpoint in AdminOnlyEndpoints()
        select new[] { role[0], endpoint[0], endpoint[1] };

    [Theory]
    [MemberData(nameof(ProtectedEndpoints))]
    public async Task Anonymous_CallingProtectedEndpoint_Returns401(string method, string url)
    {
        using var client = factory.CreateClient();

        var response = await client.SendAsync(Request(method, url));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [MemberData(nameof(EveryRoleAgainstEveryAdminEndpoint))]
    public async Task NonAdministrator_CallingAdminEndpoint_Returns403(string email, string method, string url)
    {
        using var client = await factory.SignedInAsClientAsync(email);

        var response = await client.SendAsync(Request(method, url));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UnknownRoute_WithoutToken_Returns401NotTheExistenceOfTheRoute()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/does-not-exist");

        // Deny-by-default fallback policy: anonymous callers learn nothing about routes.
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task HealthProbe_IsAnonymous()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static HttpRequestMessage Request(string method, string url)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method is "POST" or "PUT")
        {
            request.Content = JsonContent.Create(new { role = "QA" });
        }

        return request;
    }
}
