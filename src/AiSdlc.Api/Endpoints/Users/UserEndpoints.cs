using AiSdlc.Api.Http;
using AiSdlc.Api.Security;
using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using AiSdlc.Application.Users.ListUsers;
using AiSdlc.Application.Users.SetUserActive;
using AiSdlc.Application.Users.SetUserRole;

namespace AiSdlc.Api.Endpoints.Users;

/// <summary>User administration. Only the Admin role may call these.</summary>
public static class UserEndpoints
{
    public static RouteGroupBuilder MapUserEndpoints(this RouteGroupBuilder routes)
    {
        var group = routes.MapGroup("/users")
            .WithTags("Users")
            .RequireAuthorization(AuthorizationPolicies.UserAdministration);

        group.MapGet("/", ListAsync)
            .Produces<PagedResult<UserDto>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPut("/{userId:guid}/role", SetRoleAsync)
            .Produces<UserDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{userId:guid}/deactivate", (Guid userId, SetUserActiveHandler handler, CancellationToken ct) =>
                SetActiveAsync(new SetUserActive(userId, false), handler, ct))
            .Produces<UserDto>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{userId:guid}/activate", (Guid userId, SetUserActiveHandler handler, CancellationToken ct) =>
                SetActiveAsync(new SetUserActive(userId, true), handler, ct))
            .Produces<UserDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return routes;
    }

    private static async Task<IResult> ListAsync(
        ListUsersHandler handler,
        CancellationToken cancellationToken,
        int page = ListUsers.FirstPage,
        int pageSize = ListUsers.DefaultPageSize) =>
        (await handler.HandleAsync(new ListUsers(page, pageSize), cancellationToken)).ToHttpResult();

    private static async Task<IResult> SetRoleAsync(
        Guid userId, SetUserRoleRequest request, SetUserRoleHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new SetUserRole(userId, request.Role), cancellationToken)).ToHttpResult();

    private static async Task<IResult> SetActiveAsync(
        SetUserActive command, SetUserActiveHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(command, cancellationToken)).ToHttpResult();
}
