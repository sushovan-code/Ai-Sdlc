using System.Security.Claims;
using AiSdlc.Application.Abstractions;
using AiSdlc.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace AiSdlc.Api.Security;

/// <summary>
/// Runs after a token is validated. A deactivated account is rejected immediately, and the role is taken from the
/// database so a role change applies without waiting for the token to expire.
/// </summary>
internal static class AccountStatusCheck
{
    public static async Task ValidateAsync(TokenValidatedContext context)
    {
        var subject = context.Principal?.FindFirstValue(JwtClaims.Subject);
        if (!Guid.TryParse(subject, out var userId))
        {
            context.Fail("Token has no valid subject.");
            return;
        }

        var accounts = context.HttpContext.RequestServices.GetRequiredService<IUserAccounts>();
        var user = await accounts.FindAsync(userId, context.HttpContext.RequestAborted);
        if (user is null || !user.IsActive)
        {
            context.Fail("Account is unknown or disabled.");
            return;
        }

        if (context.Principal?.Identity is ClaimsIdentity identity)
        {
            foreach (var claim in identity.FindAll(JwtClaims.Role).ToList())
            {
                identity.RemoveClaim(claim);
            }

            identity.AddClaim(new Claim(JwtClaims.Role, user.Role));
        }
    }
}
