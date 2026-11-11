using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Common;
using AiSdlc.Application.Projects;
using AiSdlc.Domain.Projects;
using AiSdlc.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiSdlc.Infrastructure.Projects;

/// <summary>EF Core adapter for <see cref="IProjectRepository"/>.</summary>
internal sealed class ProjectRepository(AiSdlcDbContext db) : IProjectRepository
{
    public async Task AddAsync(Project project, CancellationToken cancellationToken) =>
        await db.Projects.AddAsync(project, cancellationToken);

    public Task<Project?> GetAsync(Guid projectId, CancellationToken cancellationToken) =>
        db.Projects
            .Include(p => p.Members)
            .Include(p => p.Files)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    public async Task<PagedResult<ProjectSummaryDto>> ListForMemberAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var mine = db.Projects.AsNoTracking().Where(p => p.Members.Any(m => m.UserId == userId));
        var total = await mine.CountAsync(cancellationToken);

        var items = await mine
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProjectSummaryDto(
                p.Id,
                p.Name,
                p.Description,
                p.Stage,
                p.CreatedAtUtc,
                p.Members.Count(),
                p.Files.Any(f => f.Kind == FileKind.Requirements && f.DeletedAtUtc == null),
                p.Files.Any(f => f.Kind == FileKind.TeamTemplate && f.DeletedAtUtc == null)))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProjectSummaryDto>(items, page, pageSize, total);
    }
}
