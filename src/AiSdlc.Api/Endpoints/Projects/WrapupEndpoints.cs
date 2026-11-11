using System.Text;
using AiSdlc.Api.Http;
using AiSdlc.Application.Common;
using AiSdlc.Application.Wrapup;
using AiSdlc.Application.Wrapup.GetAudit;
using AiSdlc.Application.Wrapup.GetReport;
using AiSdlc.Application.Wrapup.GetTraceability;

namespace AiSdlc.Api.Endpoints.Projects;

/// <summary>The wrap-up views of a project: traceability, the audit trail and the report. Any member can read them.</summary>
public static class WrapupEndpoints
{
    private const int DefaultPageSize = 25;
    private const string MarkdownContentType = "text/markdown; charset=utf-8";

    public static RouteGroupBuilder MapWrapupEndpoints(this RouteGroupBuilder routes)
    {
        var group = routes.MapGroup("/projects/{projectId:guid}").WithTags("Wrap-up");

        group.MapGet("/traceability", TraceabilityAsync)
            .RequireAuthorization()
            .Produces<TraceabilityDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/audit", AuditAsync)
            .RequireAuthorization()
            .Produces<PagedResult<AuditEntryDto>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/report", ReportAsync)
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK, contentType: MarkdownContentType)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return routes;
    }

    private static async Task<IResult> TraceabilityAsync(
        Guid projectId, GetTraceabilityHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new GetTraceability(projectId), cancellationToken)).ToHttpResult();

    private static async Task<IResult> AuditAsync(
        Guid projectId,
        GetAuditHandler handler,
        CancellationToken cancellationToken,
        int page = GetAudit.FirstPage,
        int pageSize = DefaultPageSize) =>
        (await handler.HandleAsync(new GetAudit(projectId, page, pageSize), cancellationToken)).ToHttpResult();

    private static async Task<IResult> ReportAsync(
        Guid projectId, GetReportHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new GetReport(projectId), cancellationToken))
        .ToHttpResult(file => Results.File(Encoding.UTF8.GetBytes(file.Content), MarkdownContentType, file.FileName));
}
