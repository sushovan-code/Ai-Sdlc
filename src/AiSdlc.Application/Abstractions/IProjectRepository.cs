using AiSdlc.Application.Common;
using AiSdlc.Application.Projects;
using AiSdlc.Domain.Projects;

namespace AiSdlc.Application.Abstractions;

/// <summary>Persistence for the <see cref="Project"/> aggregate plus the project list read model.</summary>
public interface IProjectRepository
{
    Task AddAsync(Project project, CancellationToken cancellationToken);

    /// <summary>Loads the project with its members and files, tracked for changes.</summary>
    Task<Project?> GetAsync(Guid projectId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>Projects the user is a member of, newest first.</summary>
    Task<PagedResult<ProjectSummaryDto>> ListForMemberAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken);
}
