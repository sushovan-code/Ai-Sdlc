using AiSdlc.Domain.Projects;

namespace AiSdlc.Api.Endpoints.Projects;

/// <summary>The kebab-case route segment for each file kind.</summary>
internal static class FileKindRoutes
{
    public const string Requirements = "requirements";
    public const string TeamTemplate = "team-template";

    public static bool TryParse(string segment, out FileKind kind)
    {
        switch (segment)
        {
            case Requirements:
                kind = FileKind.Requirements;
                return true;
            case TeamTemplate:
                kind = FileKind.TeamTemplate;
                return true;
            default:
                kind = default;
                return false;
        }
    }
}
