using AiSdlc.Application.Common;

namespace AiSdlc.Application.Abstractions;

/// <summary>Extracts plain text from an uploaded requirements document. The adapter hides the document library.</summary>
public interface IRequirementsReader
{
    /// <param name="content">The stored file content.</param>
    /// <param name="extension">The validated, lower-case extension: <c>.docx</c> or <c>.txt</c>.</param>
    /// <returns>The text, or a validation error when the document cannot be read or has no text.</returns>
    Result<string> ReadText(byte[] content, string extension);
}
