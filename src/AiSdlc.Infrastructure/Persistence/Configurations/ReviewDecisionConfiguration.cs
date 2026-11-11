using AiSdlc.Application.Analysis;
using AiSdlc.Domain.Artifacts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AiSdlc.Infrastructure.Persistence.Configurations;

internal sealed class ReviewDecisionConfiguration : IEntityTypeConfiguration<ReviewDecision>
{
    private const int EnumLength = 32;

    public void Configure(EntityTypeBuilder<ReviewDecision> builder)
    {
        builder.ToTable("ReviewDecisions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(EnumLength);
        builder.Property(x => x.ReviewerRole).HasMaxLength(EnumLength).IsRequired();
        builder.Property(x => x.Comment).HasMaxLength(AiLimits.MaxFeedbackLength);
        builder.Property(x => x.DecidedAtUtc).HasConversion(new DateTimeOffsetToBinaryConverter());

        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ReviewerId).OnDelete(DeleteBehavior.Restrict);
    }
}
