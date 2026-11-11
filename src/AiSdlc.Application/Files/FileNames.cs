using System.Text;

namespace AiSdlc.Application.Files;

/// <summary>Makes an uploaded file name safe to store and show. The result is metadata only and is never used as a path.</summary>
public static class FileNames
{
    private const string Fallback = "file";
    private static readonly char[] ForbiddenCharacters = ['<', '>', ':', '"', '|', '?', '*'];

    public static string Sanitize(string? fileName)
    {
        // Drop any directory part, whichever separator the client used.
        var name = (fileName ?? string.Empty).Split('/', '\\')[^1];

        var cleaned = new StringBuilder(name.Length);
        foreach (var character in name)
        {
            if (!char.IsControl(character) && !ForbiddenCharacters.Contains(character))
            {
                cleaned.Append(character);
            }
        }

        var result = cleaned.ToString().Trim().Trim('.');
        if (result.Length == 0)
        {
            return Fallback;
        }

        return result.Length <= FileRules.MaxFileNameLength ? result : Shorten(result);
    }

    public static string ExtensionOf(string fileName) => Path.GetExtension(fileName).ToLowerInvariant();

    /// <summary>Cuts the base name but keeps the extension so the type stays visible.</summary>
    private static string Shorten(string name)
    {
        var extension = Path.GetExtension(name);
        var baseName = name[..(FileRules.MaxFileNameLength - extension.Length)];
        return baseName + extension;
    }
}
