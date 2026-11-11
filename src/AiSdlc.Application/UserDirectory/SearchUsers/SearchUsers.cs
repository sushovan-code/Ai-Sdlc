namespace AiSdlc.Application.UserDirectory.SearchUsers;

public sealed record SearchUsers(string? Query)
{
    public const int MaxQueryLength = 100;
    public const int ResultLimit = 20;
}
