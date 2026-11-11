using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using AiSdlc.Application.Projects;
using AiSdlc.Domain.Auditing;

namespace AiSdlc.Application.Wrapup.GetReport;

/// <summary>Any project member can take the report. Each download is audited.</summary>
public sealed class GetReportHandler(
    ICurrentUser currentUser, IProjectRepository projects, IAuditLog audit, TimeProvider time)
{
    public async Task<Result<ReportFile>> HandleAsync(GetReport query, CancellationToken cancellationToken)
    {
        var opened = await ProjectAccess.OpenAsync(projects, currentUser, query.ProjectId, false, cancellationToken);
        if (opened.IsFailure)
        {
            return Result<ReportFile>.Failure(opened.Error);
        }

        var project = opened.Value;
        var content = ReportBuilder.Build(project, time.GetUtcNow());
        await audit.WriteAsync(
            new AuditRecord(
                AuditActions.ReportDownloaded,
                AuditOutcome.Success,
                currentUser.UserId,
                currentUser.Email,
                $"{project.Id}: report"),
            cancellationToken);

        return Result<ReportFile>.Success(new ReportFile(FileNameFor(project.Name), content));
    }

    /// <summary>A safe file name built from the project name; the name is never used as a path.</summary>
    private static string FileNameFor(string projectName)
    {
        var slug = new string(projectName.Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-').ToArray()).Trim('-');
        return $"{(slug.Length == 0 ? "project" : slug)}-report.md";
    }
}
