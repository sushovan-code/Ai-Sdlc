using AiSdlc.Application.Analysis;
using AiSdlc.Domain.Artifacts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AiSdlc.Infrastructure.Persistence.Configurations;

internal sealed class ArtifactVersionConfiguration : IEntityTypeConfiguration<ArtifactVersion>
{
    private const int EnumLength = 32;
    private const int ModelLength = 100;
    private const int PromptVersionLength = 50;

    public void Configure(EntityTypeBuilder<ArtifactVersion> builder)
    {
        builder.ToTable("ArtifactVersions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Stage).HasConversion<string>().HasMaxLength(EnumLength);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(EnumLength);
        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(EnumLength);
        builder.Property(x => x.ContentJson).IsRequired();
        builder.Property(x => x.Model).HasMaxLength(ModelLength).IsRequired();
        builder.Property(x => x.PromptVersion).HasMaxLength(PromptVersionLength).IsRequired();
        builder.Property(x => x.Feedback).HasMaxLength(AiLimits.MaxFeedbackLength);
        builder.Property(x => x.CreatedAtUtc).HasConversion(new DateTimeOffsetToBinaryConverter());

        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Decisions).WithOne().HasForeignKey(x => x.ArtifactVersionId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Decisions).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Versions are numbered per project and stage and never reused.
        builder.HasIndex(x => new { x.ProjectId, x.Stage, x.Version }).IsUnique();
    }
}
