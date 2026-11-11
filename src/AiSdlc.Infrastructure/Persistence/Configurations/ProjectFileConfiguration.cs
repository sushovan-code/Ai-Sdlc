using AiSdlc.Application.Files;
using AiSdlc.Domain.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AiSdlc.Infrastructure.Persistence.Configurations;

internal sealed class ProjectFileConfiguration : IEntityTypeConfiguration<ProjectFile>
{
    private const int Sha256HexLength = 64;
    private const int MaxStorageKeyLength = 200;

    public void Configure(EntityTypeBuilder<ProjectFile> builder)
    {
        builder.ToTable("ProjectFiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.FileName).HasMaxLength(FileRules.MaxFileNameLength).IsRequired();
        builder.Property(x => x.Sha256).HasMaxLength(Sha256HexLength).IsRequired();
        builder.Property(x => x.StorageKey).HasMaxLength(MaxStorageKeyLength).IsRequired();
        builder.Property(x => x.UploadedAtUtc).HasConversion(new DateTimeOffsetToBinaryConverter());
        builder.Property(x => x.DeletedAtUtc).HasConversion(new DateTimeOffsetToBinaryConverter());
        builder.Ignore(x => x.IsDeleted);

        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UploadedById).OnDelete(DeleteBehavior.Restrict);

        // Versions are numbered per project and kind and never reused.
        builder.HasIndex(x => new { x.ProjectId, x.Kind, x.Version }).IsUnique();
    }
}
