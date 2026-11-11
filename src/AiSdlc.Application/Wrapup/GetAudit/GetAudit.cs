namespace AiSdlc.Application.Wrapup.GetAudit;

public sealed record GetAudit(Guid ProjectId, int Page, int PageSize)
{
    public const int FirstPage = 1;
    public const int MaxPageSize = 100;
}
