using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using AiSdlc.Application.Projects;

namespace AiSdlc.Application.Wrapup.GetAudit;

/// <summary>Any project member can read the audit trail of that project: uploads, AI calls, decisions and edits.</summary>
public sealed class GetAuditHandler(ICurrentUser currentUser, IProjectRepository projects, IAuditQuery audit)
{
    public async Task<Result<PagedResult<AuditEntryDto>>> HandleAsync(GetAudit query, CancellationToken cancellationToken)
    {
        if (query.Page < GetAudit.FirstPage || query.PageSize is < 1 or > GetAudit.MaxPageSize)
        {
            return Result<PagedResult<AuditEntryDto>>.Failure(
                Error.Validation("page", $"Page must be 1 or more and the page size between 1 and {GetAudit.MaxPageSize}."));
        }

        var opened = await ProjectAccess.OpenAsync(projects, currentUser, query.ProjectId, false, cancellationToken);
        if (opened.IsFailure)
        {
            return Result<PagedResult<AuditEntryDto>>.Failure(opened.Error);
        }

        // The trail names people and uploaded file names, so it is for the roles that review the work, not every member.
        if (!GetAudit.ReaderRoles.Contains(currentUser.Role))
        {
            return Result<PagedResult<AuditEntryDto>>.Failure(ProjectErrors.AuditRestricted);
        }

        var page = await audit.ListForProjectAsync(query.ProjectId, query.Page, query.PageSize, cancellationToken);
        return Result<PagedResult<AuditEntryDto>>.Success(page);
    }
}
