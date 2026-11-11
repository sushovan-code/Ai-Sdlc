using AiSdlc.Api.Http;
using AiSdlc.Api.Security;
using AiSdlc.Application.Analysis;
using AiSdlc.Application.Analysis.ApproveAnalysis;
using AiSdlc.Application.Analysis.GenerateAnalysis;
using AiSdlc.Application.Analysis.GetAnalysis;
using AiSdlc.Application.Analysis.RejectAnalysis;

namespace AiSdlc.Api.Endpoints.Projects;

/// <summary>Gate 1: the AI requirements analysis. Members read; Project Managers generate and decide.</summary>
public static class AnalysisEndpoints
{
    public static RouteGroupBuilder MapAnalysisEndpoints(this RouteGroupBuilder routes)
    {
        var group = routes.MapGroup("/projects/{projectId:guid}/analysis").WithTags("Analysis");

        group.MapGet("/", GetAsync)
            .RequireAuthorization()
            .Produces<AnalysisOverviewDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/generate", GenerateAsync)
            .RequireAuthorization(AuthorizationPolicies.ProjectManagement)
            .RequireRateLimiting(RateLimitPolicies.Writes)
            .Produces<AnalysisVersionDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapPost("/versions/{version:int}/approve", ApproveAsync)
            .RequireAuthorization(AuthorizationPolicies.ProjectManagement)
            .RequireRateLimiting(RateLimitPolicies.Writes)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/versions/{version:int}/reject", RejectAsync)
            .RequireAuthorization(AuthorizationPolicies.ProjectManagement)
            .RequireRateLimiting(RateLimitPolicies.Writes)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return routes;
    }

    private static async Task<IResult> GetAsync(
        Guid projectId, GetAnalysisHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new GetAnalysis(projectId), cancellationToken)).ToHttpResult();

    private static async Task<IResult> GenerateAsync(
        Guid projectId,
        GenerateAnalysisRequest? request,
        GenerateAnalysisHandler handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new GenerateAnalysis(projectId, request?.Feedback), cancellationToken)).ToHttpResult();

    private static async Task<IResult> ApproveAsync(
        Guid projectId,
        int version,
        DecideAnalysisRequest? request,
        ApproveAnalysisHandler handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ApproveAnalysis(projectId, version, request?.Comment), cancellationToken)).ToHttpResult();

    private static async Task<IResult> RejectAsync(
        Guid projectId,
        int version,
        DecideAnalysisRequest? request,
        RejectAnalysisHandler handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new RejectAnalysis(projectId, version, request?.Comment ?? string.Empty), cancellationToken)).ToHttpResult();
}
