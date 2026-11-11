using AiSdlc.Application.Abstractions;
using AiSdlc.Domain.Auditing;
using AiSdlc.Infrastructure.Persistence;

namespace AiSdlc.Infrastructure.Auditing;

/// <summary>Writes audit rows to the application database.</summary>
internal sealed class AuditLog(AiSdlcDbContext db, TimeProvider time) : IAuditLog
{
    public async Task WriteAsync(AuditRecord record, CancellationToken cancellationToken)
    {
        db.AuditEntries.Add(new AuditEntry
        {
            Id = Guid.NewGuid(),
            OccurredAtUtc = time.GetUtcNow(),
            ActorId = record.ActorId,
            ActorEmail = record.ActorEmail,
            Action = record.Action,
            Outcome = record.Outcome,
            Details = record.Details,
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
