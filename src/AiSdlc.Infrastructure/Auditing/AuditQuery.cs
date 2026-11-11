using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using AiSdlc.Application.Wrapup;
using AiSdlc.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiSdlc.Infrastructure.Auditing;

/// <summary>Reads a project's audit entries. Entries name their project at the start of the details text.</summary>
internal sealed class AuditQuery(AiSdlcDbContext db) : IAuditQuery
{
    public async Task<PagedResult<AuditEntryDto>> ListForProjectAsync(
        Guid projectId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var prefix = projectId.ToString();
        var entries = db.AuditEntries.AsNoTracking().Where(e => e.Details != null && e.Details.StartsWith(prefix));
        var total = await entries.CountAsync(cancellationToken);

        var items = await entries
            .OrderByDescending(e => e.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new AuditEntryDto(e.OccurredAtUtc, e.ActorEmail, e.Action, e.Outcome, e.Details))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditEntryDto>(items, page, pageSize, total);
    }
}
