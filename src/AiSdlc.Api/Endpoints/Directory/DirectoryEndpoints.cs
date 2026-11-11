using AiSdlc.Api.Http;
using AiSdlc.Api.Security;
using AiSdlc.Application.Abstractions;
using AiSdlc.Application.UserDirectory.SearchUsers;

namespace AiSdlc.Api.Endpoints.Directory;

/// <summary>A small people finder so a Project Manager can pick colleagues to add to a project.</summary>
public static class DirectoryEndpoints
{
    public static RouteGroupBuilder MapDirectoryEndpoints(this RouteGroupBuilder routes)
    {
        var group = routes.MapGroup("/directory").WithTags("Directory");

        group.MapGet("/users", SearchAsync)
            .RequireAuthorization(AuthorizationPolicies.ProjectManagement)
            .Produces<IReadOnlyList<UserDto>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return routes;
    }

    private static async Task<IResult> SearchAsync(
        SearchUsersHandler handler, CancellationToken cancellationToken, string? query = null) =>
        (await handler.HandleAsync(new SearchUsers(query), cancellationToken)).ToHttpResult();
}
