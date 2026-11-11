using System.Text.Json;
using AiSdlc.Application.Team.Import;
using AiSdlc.Domain.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiSdlc.Infrastructure.Persistence.Configurations;

internal sealed class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
    private const int MaxSkillsJsonLength = 2000;

    public void Configure(EntityTypeBuilder<TeamMember> builder)
    {
        builder.ToTable("TeamMembers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).HasMaxLength(TeamSheetRules.MaxNameLength).IsRequired();
        builder.Property(x => x.Role).HasMaxLength(TeamSheetRules.MaxRoleLength).IsRequired();
        builder.Property(x => x.CapacityHours).HasPrecision(7, 2);
        builder.Property(x => x.AvailabilityHours).HasPrecision(7, 2);

        // Skills are a short list; keep them in one column as JSON.
        builder.Property(x => x.Skills)
            .HasConversion(
                skills => JsonSerializer.Serialize(skills, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null)!,
                new ValueComparer<IReadOnlyList<string>>(
                    (a, b) => a!.SequenceEqual(b!),
                    list => list.Aggregate(0, (hash, skill) => HashCode.Combine(hash, skill.GetHashCode(StringComparison.Ordinal))),
                    list => list.ToList()))
            .HasMaxLength(MaxSkillsJsonLength);

        builder.HasOne<ProjectFile>().WithMany().HasForeignKey(x => x.ProjectFileId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.ProjectId);
    }
}
