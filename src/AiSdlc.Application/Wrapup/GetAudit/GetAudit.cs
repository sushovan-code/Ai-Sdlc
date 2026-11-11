using AiSdlc.Domain.Security;

namespace AiSdlc.Application.Wrapup.GetAudit;

public sealed record GetAudit(Guid ProjectId, int Page, int PageSize)
{
    public const int FirstPage = 1;
    public const int MaxPageSize = 100;

    /// <summary>The roles that may read a project audit trail. Developers see the project, tasks and traceability.</summary>
    public static readonly IReadOnlySet<string?> ReaderRoles = new HashSet<string?>
    {
        AppRoles.ProjectManager,
        AppRoles.Architect,
        AppRoles.QA,
    };
}
