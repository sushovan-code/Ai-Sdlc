using AiSdlc.Api.Http;
using AiSdlc.Api.Security;
using AiSdlc.Application.Common;
using AiSdlc.Application.Files;
using AiSdlc.Application.Files.DeleteProjectFile;
using AiSdlc.Application.Files.DownloadProjectFile;
using AiSdlc.Application.Files.UploadProjectFile;
using AiSdlc.Application.Projects;
using Microsoft.AspNetCore.Http.Metadata;

namespace AiSdlc.Api.Endpoints.Projects;

/// <summary>Versioned input files of a project. Uploads and deletes need the Project Manager role; downloads need membership.</summary>
public static class ProjectFileEndpoints
{
    /// <summary>The file limit plus room for the multipart envelope.</summary>
    public const long UploadRequestLimitBytes = FileRules.MaxFileBytes + (1024 * 1024);

    private const string UnknownKindCode = "project.unknown-file-kind";

    public static RouteGroupBuilder MapProjectFileEndpoints(this RouteGroupBuilder routes)
    {
        var group = routes.MapGroup("/projects/{projectId:guid}/files").WithTags("Project files");

        group.MapPost("/{kind}", UploadAsync)
            .RequireAuthorization(AuthorizationPolicies.ProjectManagement)
            .RequireRateLimiting(RateLimitPolicies.Writes)
            .WithMetadata(new RequestSizeLimitMetadata(UploadRequestLimitBytes))
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<ProjectFileDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{fileId:guid}/download", DownloadAsync)
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK, contentType: "application/octet-stream")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{fileId:guid}", DeleteAsync)
            .RequireAuthorization(AuthorizationPolicies.ProjectManagement)
            .RequireRateLimiting(RateLimitPolicies.Writes)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return routes;
    }

    private static async Task<IResult> UploadAsync(
        Guid projectId,
        string kind,
        IFormFile file,
        UploadProjectFileHandler handler,
        CancellationToken cancellationToken)
    {
        if (!FileKindRoutes.TryParse(kind, out var fileKind))
        {
            return Results.Problem(
                title: "Unknown file kind. Use 'requirements' or 'team-template'.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: new Dictionary<string, object?> { ["code"] = UnknownKindCode });
        }

        await using var content = file.OpenReadStream();
        var result = await handler.HandleAsync(
            new UploadProjectFile(projectId, fileKind, file.FileName, content), cancellationToken);
        return result.ToHttpResult(uploaded => Results.Created($"/api/v1/projects/{projectId}", uploaded));
    }

    private static async Task<IResult> DownloadAsync(
        Guid projectId, Guid fileId, DownloadProjectFileHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DownloadProjectFile(projectId, fileId), cancellationToken);
        return result.ToHttpResult(download =>
            Results.File(download.Content, download.ContentType, download.FileName));
    }

    private static async Task<IResult> DeleteAsync(
        Guid projectId, Guid fileId, DeleteProjectFileHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new DeleteProjectFile(projectId, fileId), cancellationToken)).ToHttpResult();

    private sealed class RequestSizeLimitMetadata(long maxRequestBodySize) : IRequestSizeLimitMetadata
    {
        public long? MaxRequestBodySize { get; } = maxRequestBodySize;
    }
}
