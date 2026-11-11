using AiSdlc.Api.Http;
using AiSdlc.Api.Security;
using AiSdlc.Application.Stages;
using AiSdlc.Application.Stages.Content;
using AiSdlc.Application.Stages.DecideStage;
using AiSdlc.Application.Stages.EditStage;
using AiSdlc.Application.Stages.GenerateStage;
using AiSdlc.Application.Stages.GetStage;
using AiSdlc.Domain.Artifacts;
using Microsoft.AspNetCore.Mvc;

namespace AiSdlc.Api.Endpoints.Projects;

/// <summary>
/// Gates 2 to 4: architecture, tasks and test cases. Members read; only the stage reviewer (Architect, Project Manager
/// or QA) generates, edits and decides, which the handlers enforce against the route's stage.
/// </summary>
public static class StageEndpoints
{
    private const long MaxEditBodyBytes = 512 * 1024;

    private static readonly Dictionary<string, ArtifactStage> Slugs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["architecture"] = ArtifactStage.Architecture,
        ["tasks"] = ArtifactStage.Tasks,
        ["test-cases"] = ArtifactStage.TestCases,
    };

    public static RouteGroupBuilder MapStageEndpoints(this RouteGroupBuilder routes)
    {
        var group = routes.MapGroup("/projects/{projectId:guid}/stages/{stage}").WithTags("Stages");

        group.MapGet("/", GetAsync)
            .RequireAuthorization()
            .Produces<StageOverviewDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/generate", GenerateAsync)
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicies.AiGeneration)
            .Produces<StageVersionDto>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapPost("/versions/{version:int}/approve", ApproveAsync)
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicies.Writes)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/versions/{version:int}/reject", RejectAsync)
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicies.Writes)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/edit", EditAsync)
            .RequireAuthorization()
            .WithMetadata(new RequestSizeLimitAttribute(MaxEditBodyBytes))
            .RequireRateLimiting(RateLimitPolicies.Writes)
            .Produces<StageVersionDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return routes;
    }

    private static async Task<IResult> GetAsync(
        Guid projectId, string stage, GetStageHandler handler, CancellationToken cancellationToken) =>
        Slugs.TryGetValue(stage, out var kind)
            ? (await handler.HandleAsync(new GetStage(projectId, kind), cancellationToken)).ToHttpResult()
            : Results.NotFound();

    private static async Task<IResult> GenerateAsync(
        Guid projectId,
        string stage,
        GenerateAnalysisRequest? request,
        GenerateStageHandler handler,
        CancellationToken cancellationToken) =>
        Slugs.TryGetValue(stage, out var kind)
            ? (await handler.HandleAsync(new GenerateStage(projectId, kind, request?.Feedback), cancellationToken)).ToHttpResult()
            : Results.NotFound();

    private static async Task<IResult> ApproveAsync(
        Guid projectId,
        string stage,
        int version,
        DecideAnalysisRequest? request,
        DecideStageHandler handler,
        CancellationToken cancellationToken) =>
        Slugs.TryGetValue(stage, out var kind)
            ? (await handler.ApproveAsync(new DecideStage(projectId, kind, version, request?.Comment), cancellationToken)).ToHttpResult()
            : Results.NotFound();

    private static async Task<IResult> RejectAsync(
        Guid projectId,
        string stage,
        int version,
        DecideAnalysisRequest? request,
        DecideStageHandler handler,
        CancellationToken cancellationToken) =>
        Slugs.TryGetValue(stage, out var kind)
            ? (await handler.RejectAsync(new DecideStage(projectId, kind, version, request?.Comment), cancellationToken)).ToHttpResult()
            : Results.NotFound();

    private static async Task<IResult> EditAsync(
        Guid projectId,
        string stage,
        StageContent content,
        EditStageHandler handler,
        CancellationToken cancellationToken) =>
        Slugs.TryGetValue(stage, out var kind)
            ? (await handler.HandleAsync(new EditStage(projectId, kind, content), cancellationToken)).ToHttpResult()
            : Results.NotFound();
}
