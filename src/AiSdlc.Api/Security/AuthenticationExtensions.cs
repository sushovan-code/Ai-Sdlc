using AiSdlc.Application.Abstractions;
using AiSdlc.Domain.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace AiSdlc.Api.Security;

public static class AuthenticationExtensions
{
    /// <summary>JWT bearer authentication, deny-by-default fallback policy and the named role policies.</summary>
    public static IServiceCollection AddApiSecurity(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(AuthorizationPolicies.UserAdministration, policy => policy.RequireRole(AppRoles.Admin));

        return services;
    }
}
