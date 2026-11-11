using AiSdlc.Api.Http;
using AiSdlc.Api.Security;
using AiSdlc.Application.Common;
using AiSdlc.Application.Projects;
using AiSdlc.Application.Projects.AddProjectMember;
using AiSdlc.Application.Projects.CreateProject;
using AiSdlc.Application.Projects.GetProject;
using AiSdlc.Application.Projects.ListProjects;
using AiSdlc.Application.Projects.RemoveProjectMember;

namespace AiSdlc.Api.Endpoints.Projects;

/// <summary>Projects and their members. Reads need membership; changes need the Project Manager role as well.</summary>
public static class ProjectEndpoints
{
    public static RouteGroupBuilder MapProjectEndpoints(this RouteGroupBuilder routes)
    {
        var group = routes.MapGroup("/projects").WithTags("Projects");

        group.MapPost("/", CreateAsync)
            .RequireAuthorization(AuthorizationPolicies.ProjectManagement)
            .RequireRateLimiting(RateLimitPolicies.Writes)
            .Produces<ProjectSummaryDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/", ListAsync)
            .RequireAuthorization()
            .Produces<PagedResult<ProjectSummaryDto>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{projectId:guid}", GetAsync)
            .RequireAuthorization()
            .Produces<ProjectDetailDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{projectId:guid}/members", AddMemberAsync)
            .RequireAuthorization(AuthorizationPolicies.ProjectManagement)
            .RequireRateLimiting(RateLimitPolicies.Writes)
            .Produces<ProjectMemberDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{projectId:guid}/members/{userId:guid}", RemoveMemberAsync)
            .RequireAuthorization(AuthorizationPolicies.ProjectManagement)
            .RequireRateLimiting(RateLimitPolicies.Writes)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return routes;
    }

    private static async Task<IResult> CreateAsync(
        CreateProjectRequest request, CreateProjectHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new CreateProject(request.Name, request.Description), cancellationToken);
        return result.ToHttpResult(project => Results.Created($"/api/v1/projects/{project.Id}", project));
    }

    private static async Task<IResult> ListAsync(
        ListProjectsHandler handler,
        CancellationToken cancellationToken,
        int page = ListProjects.FirstPage,
        int pageSize = ListProjects.DefaultPageSize) =>
        (await handler.HandleAsync(new ListProjects(page, pageSize), cancellationToken)).ToHttpResult();

    private static async Task<IResult> GetAsync(
        Guid projectId, GetProjectHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new GetProject(projectId), cancellationToken)).ToHttpResult();

    private static async Task<IResult> AddMemberAsync(
        Guid projectId,
        AddProjectMemberRequest request,
        AddProjectMemberHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new AddProjectMember(projectId, request.UserId), cancellationToken);
        return result.ToHttpResult(member => Results.Created($"/api/v1/projects/{projectId}", member));
    }

    private static async Task<IResult> RemoveMemberAsync(
        Guid projectId, Guid userId, RemoveProjectMemberHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new RemoveProjectMember(projectId, userId), cancellationToken)).ToHttpResult();
}
