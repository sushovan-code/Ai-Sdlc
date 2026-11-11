using AiSdlc.Application.Common;
using AiSdlc.Application.Wrapup;

namespace AiSdlc.Application.Abstractions;

/// <summary>Reads the audit trail back. Writing is <see cref="IAuditLog"/>; the two are kept apart on purpose.</summary>
public interface IAuditQuery
{
    /// <summary>The entries whose details name the project, newest first.</summary>
    Task<PagedResult<AuditEntryDto>> ListForProjectAsync(
        Guid projectId, int page, int pageSize, CancellationToken cancellationToken);
}
