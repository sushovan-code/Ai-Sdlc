using AiSdlc.Domain.Projects;

namespace AiSdlc.Application.Files;

/// <summary>What may be uploaded for each kind of project file.</summary>
public static class FileRules
{
    public const long MaxFileBytes = 10L * 1024 * 1024;

    /// <summary>Open XML files are zip archives; refuse ones that declare an unreasonable amount of data inside.</summary>
    public const long MaxUncompressedBytes = 200L * 1024 * 1024;

    public const int MaxArchiveEntries = 5_000;
    public const int MaxFileNameLength = 150;

    public const string DocxContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    public const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public const string TextContentType = "text/plain; charset=utf-8";

    private static readonly Dictionary<FileKind, string[]> AllowedExtensions =
        new()
        {
            [FileKind.Requirements] = [".docx", ".txt"],
            [FileKind.TeamTemplate] = [".xlsx"],
        };

    public static IReadOnlyList<string> ExtensionsFor(FileKind kind) => AllowedExtensions[kind];

    public static bool IsAllowed(FileKind kind, string extension) =>
        AllowedExtensions[kind].Contains(extension, StringComparer.OrdinalIgnoreCase);

    /// <summary>The content type is decided here from the validated extension, never taken from the client.</summary>
    public static string ContentTypeFor(string extension) => extension.ToLowerInvariant() switch
    {
        ".docx" => DocxContentType,
        ".xlsx" => XlsxContentType,
        _ => TextContentType,
    };
}
