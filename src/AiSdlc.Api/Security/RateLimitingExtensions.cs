using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace AiSdlc.Api.Security;

public static class RateLimitingExtensions
{
    private const string UnknownClient = "unknown";

    /// <summary>A per-client fixed window on the anonymous auth endpoints to slow credential stuffing.</summary>
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ApiRateLimitOptions>(configuration.GetSection(ApiRateLimitOptions.SectionName));
        services.AddRateLimiter(_ => { });

        services.AddOptions<RateLimiterOptions>()
            .Configure<IOptions<ApiRateLimitOptions>>((limiter, settings) =>
            {
                limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                limiter.AddPolicy(RateLimitPolicies.Auth, httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? UnknownClient,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = settings.Value.AuthPermitLimit,
                            Window = TimeSpan.FromSeconds(settings.Value.AuthWindowSeconds),
                            QueueLimit = 0,
                        }));
            });

        return services;
    }
}
