using AiSdlc.Domain.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AiSdlc.Infrastructure.Persistence.Configurations;

internal sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    private const int MaxActionLength = 100;
    private const int MaxEmailLength = 254;
    private const int MaxDetailsLength = 1000;

    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("AuditEntries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        // SQLite cannot order DateTimeOffset natively, so store it as a sortable number.
        builder.Property(x => x.OccurredAtUtc).HasConversion(new DateTimeOffsetToBinaryConverter());
        builder.Property(x => x.Action).HasMaxLength(MaxActionLength);
        builder.Property(x => x.ActorEmail).HasMaxLength(MaxEmailLength);
        builder.Property(x => x.Details).HasMaxLength(MaxDetailsLength);
        builder.Property(x => x.Outcome).HasConversion<string>();

        builder.HasIndex(x => x.OccurredAtUtc);
        builder.HasIndex(x => x.ActorId);
    }
}
