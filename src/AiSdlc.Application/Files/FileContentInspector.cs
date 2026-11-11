using System.IO.Compression;
using System.Text;
using AiSdlc.Application.Common;
using AiSdlc.Domain.Projects;

namespace AiSdlc.Application.Files;

/// <summary>
/// Checks that the bytes really are what the extension claims: the zip signature, the required part inside an
/// Open XML package, no macro payload, no absurd archive size, and clean UTF-8 for text. Nothing is decompressed.
/// </summary>
public static class FileContentInspector
{
    public const string RejectedCode = "file.rejected";

    private const string WordDocumentPart = "word/document.xml";
    private const string WorkbookPart = "xl/workbook.xml";
    private const string MacroProject = "vbaProject.bin";
    private static readonly byte[] ZipSignature = [0x50, 0x4B, 0x03, 0x04];

    public static Result Inspect(FileKind kind, string extension, ReadOnlySpan<byte> content)
    {
        if (content.IsEmpty)
        {
            return Reject("The file is empty.");
        }

        return (kind, extension) switch
        {
            (FileKind.Requirements, ".docx") => InspectPackage(content, WordDocumentPart, "Word (.docx)"),
            (FileKind.Requirements, ".txt") => InspectText(content),
            (FileKind.TeamTemplate, ".xlsx") => InspectPackage(content, WorkbookPart, "Excel (.xlsx)"),
            _ => Reject($"Only {string.Join(", ", FileRules.ExtensionsFor(kind))} files are accepted here."),
        };
    }

    private static Result InspectPackage(ReadOnlySpan<byte> content, string requiredPart, string label)
    {
        if (!content.StartsWith(ZipSignature))
        {
            return Reject($"This is not a valid {label} file.");
        }

        try
        {
            using var stream = new MemoryStream(content.ToArray(), writable: false);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            return InspectEntries(archive, requiredPart, label);
        }
        catch (InvalidDataException)
        {
            return Reject($"This is not a valid {label} file.");
        }
    }

    private static Result InspectEntries(ZipArchive archive, string requiredPart, string label)
    {
        if (archive.Entries.Count > FileRules.MaxArchiveEntries)
        {
            return Reject("The file contains too many parts.");
        }

        if (archive.Entries.Sum(e => e.Length) > FileRules.MaxUncompressedBytes)
        {
            return Reject("The file expands to an unreasonable size.");
        }

        if (archive.Entries.Any(e => e.FullName.EndsWith(MacroProject, StringComparison.OrdinalIgnoreCase)))
        {
            return Reject("Files that contain macros are not accepted.");
        }

        return archive.GetEntry(requiredPart) is null
            ? Reject($"This is not a valid {label} file.")
            : Result.Success();
    }

    private static Result InspectText(ReadOnlySpan<byte> content)
    {
        if (content.Contains((byte)0))
        {
            return Reject("This does not look like a plain text file.");
        }

        try
        {
            var text = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)
                .GetString(content);
            return string.IsNullOrWhiteSpace(text) ? Reject("The file has no text in it.") : Result.Success();
        }
        catch (DecoderFallbackException)
        {
            return Reject("The text file must be UTF-8 encoded.");
        }
    }

    private static Result Reject(string message) =>
        Result.Failure(new Error(RejectedCode, message, ErrorType.Validation)
        {
            ValidationErrors = new Dictionary<string, string[]> { ["file"] = [message] },
        });
}
