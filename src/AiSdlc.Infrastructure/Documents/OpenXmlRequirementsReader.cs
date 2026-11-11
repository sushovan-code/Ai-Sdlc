using System.Text;
using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Analysis;
using AiSdlc.Application.Common;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;

namespace AiSdlc.Infrastructure.Documents;

/// <summary>Extracts the text of a <c>.docx</c> (paragraphs, including those in tables) or a UTF-8 <c>.txt</c>.</summary>
internal sealed class OpenXmlRequirementsReader(ILogger<OpenXmlRequirementsReader> logger) : IRequirementsReader
{
    private const string DocxExtension = ".docx";
    private const string EmptyMessage = "The requirements document has no readable text.";
    private const string UnreadableMessage = "The requirements document could not be read.";

    public Result<string> ReadText(byte[] content, string extension)
    {
        try
        {
            var text = extension == DocxExtension ? ReadDocx(content) : Encoding.UTF8.GetString(content);
            return string.IsNullOrWhiteSpace(text) ? Fail(EmptyMessage) : Result<string>.Success(text.Trim());
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // An untrusted file can fail inside the library in many ways; the user only needs to know it is not readable.
            logger.LogWarning(exception, "The requirements document could not be read.");
            return Fail(UnreadableMessage);
        }
    }

    private static string ReadDocx(byte[] content)
    {
        using var stream = new MemoryStream(content, writable: false);
        using var document = WordprocessingDocument.Open(stream, isEditable: false);
        var body = document.MainDocumentPart?.Document?.Body;
        if (body is null)
        {
            return string.Empty;
        }

        // Stop once the text is past what the analysis accepts, so a huge document cannot use unbounded memory.
        var text = new StringBuilder();
        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            var line = paragraph.InnerText.Trim();
            if (line.Length > 0)
            {
                text.Append(line).Append('\n');
            }

            if (text.Length > AiLimits.MaxSourceCharacters)
            {
                break;
            }
        }

        return text.ToString();
    }

    private static Result<string> Fail(string message) =>
        Result<string>.Failure(new Error(AnalysisErrors.RequirementsUnreadableCode, message, ErrorType.Validation)
        {
            ValidationErrors = new Dictionary<string, string[]> { ["file"] = [message] },
        });
}
