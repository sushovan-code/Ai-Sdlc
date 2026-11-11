namespace AiSdlc.Application.Users.ListUsers;

public sealed record ListUsers(int Page = ListUsers.FirstPage, int PageSize = ListUsers.DefaultPageSize)
{
    public const int FirstPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}
