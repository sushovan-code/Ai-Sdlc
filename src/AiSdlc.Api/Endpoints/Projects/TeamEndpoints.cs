using AiSdlc.Api.Http;
using AiSdlc.Api.Security;
using AiSdlc.Application.Team.GetTeam;
using AiSdlc.Application.Team.SetSprintCadence;

namespace AiSdlc.Api.Endpoints.Projects;

/// <summary>The project's team capacity and sprint length. Members may read; Project Managers may change the sprint length.</summary>
public static class TeamEndpoints
{
    public static RouteGroupBuilder MapTeamEndpoints(this RouteGroupBuilder routes)
    {
        var group = routes.MapGroup("/projects/{projectId:guid}").WithTags("Team");

        group.MapGet("/team", GetAsync)
            .RequireAuthorization()
            .Produces<TeamOverviewDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/sprint-cadence", SetCadenceAsync)
            .RequireAuthorization(AuthorizationPolicies.ProjectManagement)
            .RequireRateLimiting(RateLimitPolicies.Writes)
            .Produces<SprintCadenceDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return routes;
    }

    private static async Task<IResult> GetAsync(
        Guid projectId, GetTeamHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new GetTeam(projectId), cancellationToken)).ToHttpResult();

    private static async Task<IResult> SetCadenceAsync(
        Guid projectId,
        SetSprintCadenceRequest request,
        SetSprintCadenceHandler handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new SetSprintCadence(projectId, request.Weeks), cancellationToken)).ToHttpResult();
}
