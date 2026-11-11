using AiSdlc.Application.Projects.CreateProject;
using AiSdlc.Domain.Planning;
using AiSdlc.Domain.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AiSdlc.Infrastructure.Persistence.Configurations;

internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).HasMaxLength(CreateProjectValidator.MaxNameLength).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(CreateProjectValidator.MaxDescriptionLength);
        builder.Property(x => x.Stage).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.SprintWeeks).HasDefaultValue(SprintRules.DefaultWeeks);
        builder.Property(x => x.CreatedAtUtc).HasConversion(new DateTimeOffsetToBinaryConverter());

        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Members).WithOne().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Files).WithOne().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Team).WithOne().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Members).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Team).UsePropertyAccessMode(PropertyAccessMode.Field);

        // A computed view over Team (the newest sheet's people), not a second relationship.
        builder.Ignore(x => x.CurrentTeam);
        builder.Navigation(x => x.Files).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.CreatedAtUtc);
    }
}
