using AiSdlc.Api.Http;
using AiSdlc.Api.Security;
using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Auth;
using AiSdlc.Application.Auth.ChangePassword;
using AiSdlc.Application.Auth.GetCurrentUser;
using AiSdlc.Application.Auth.Login;
using AiSdlc.Application.Auth.Register;

namespace AiSdlc.Api.Endpoints.Auth;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder routes)
    {
        var group = routes.MapGroup("/auth").WithTags("Auth");

        group.MapPost("/register", RegisterAsync)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .Produces<UserDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .Produces<AuthTokenDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/me", GetCurrentUserAsync)
            .RequireAuthorization()
            .Produces<UserDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/change-password", ChangePasswordAsync)
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return routes;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request, RegisterHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new Register(request.Email, request.DisplayName, request.Password), cancellationToken);
        return result.ToHttpResult(user => Results.Created($"/api/v1/users/{user.Id}", user));
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request, LoginHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new Login(request.Email, request.Password), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetCurrentUserAsync(
        GetCurrentUserHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(cancellationToken)).ToHttpResult();

    private static async Task<IResult> ChangePasswordAsync(
        ChangePasswordRequest request, ChangePasswordHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ChangePassword(request.CurrentPassword, request.NewPassword), cancellationToken);
        return result.ToHttpResult();
    }
}
