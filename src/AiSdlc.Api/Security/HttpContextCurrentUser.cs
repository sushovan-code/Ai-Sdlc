using System.Security.Claims;
using AiSdlc.Application.Abstractions;
using AiSdlc.Infrastructure.Security;

namespace AiSdlc.Api.Security;

/// <summary>Reads the caller from the authenticated principal of the current request.</summary>
internal sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirstValue(JwtClaims.Subject), out var id) ? id : null;

    public string? Email => Principal?.FindFirstValue(JwtClaims.Email);

    public string? Role => Principal?.FindFirstValue(JwtClaims.Role);
}
