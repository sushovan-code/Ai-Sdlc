namespace AiSdlc.Application.Projects.ListProjects;

public sealed record ListProjects(int Page = ListProjects.FirstPage, int PageSize = ListProjects.DefaultPageSize)
{
    public const int FirstPage = 1;
    public const int DefaultPageSize = 24;
    public const int MaxPageSize = 100;
}
