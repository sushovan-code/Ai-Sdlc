using AiSdlc.IntegrationTests.Infrastructure;
using Shouldly;

namespace AiSdlc.IntegrationTests.Security;

[Collection(SharedApi.Name)]
public sealed class SecurityHeadersTests(ApiFactory factory)
{
    [Fact]
    public async Task Response_CarriesBaselineSecurityHeaders()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/auth/me");

        response.Headers.GetValues("X-Content-Type-Options").ShouldBe(["nosniff"]);
        response.Headers.GetValues("X-Frame-Options").ShouldBe(["DENY"]);
        response.Headers.GetValues("Referrer-Policy").ShouldBe(["no-referrer"]);
        response.Headers.GetValues("Content-Security-Policy").Single().ShouldContain("default-src 'none'");
    }

    [Fact]
    public async Task CrossOriginRequest_IsNotGivenCorsPermission()
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Add("Origin", "https://evil.example");

        var response = await client.SendAsync(request);

        response.Headers.Contains("Access-Control-Allow-Origin").ShouldBeFalse();
    }
}
