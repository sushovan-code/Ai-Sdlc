namespace AiSdlc.Application.Abstractions;

/// <summary>Append-only audit trail for security-relevant actions.</summary>
public interface IAuditLog
{
    Task WriteAsync(AuditRecord record, CancellationToken cancellationToken);
}
