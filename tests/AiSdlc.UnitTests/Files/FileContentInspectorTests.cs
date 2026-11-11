using System.Text;
using AiSdlc.Application.Files;
using AiSdlc.Domain.Projects;
using AiSdlc.UnitTests.Support;
using Shouldly;

namespace AiSdlc.UnitTests.Files;

public sealed class FileContentInspectorTests
{
    private static string RejectionOf(FileKind kind, string extension, byte[] content)
    {
        var result = FileContentInspector.Inspect(kind, extension, content);
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(FileContentInspector.RejectedCode);
        return result.Error.ValidationErrors!["file"][0];
    }

    [Fact]
    public void Docx_WithWordDocumentPart_IsAccepted()
    {
        FileContentInspector.Inspect(FileKind.Requirements, ".docx", TestFiles.Docx()).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Xlsx_WithWorkbookPart_IsAccepted()
    {
        FileContentInspector.Inspect(FileKind.TeamTemplate, ".xlsx", TestFiles.Xlsx()).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Txt_WithUtf8Text_IsAccepted()
    {
        FileContentInspector.Inspect(FileKind.Requirements, ".txt", TestFiles.Text("Café menu: crème brûlée")).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Empty_IsRejected()
    {
        RejectionOf(FileKind.Requirements, ".txt", []).ShouldContain("empty");
    }

    [Fact]
    public void Docx_ThatIsNotAZip_IsRejected()
    {
        RejectionOf(FileKind.Requirements, ".docx", Encoding.UTF8.GetBytes("just text pretending")).ShouldContain("not a valid Word");
    }

    [Fact]
    public void Docx_WithZipSignatureButCorruptBody_IsRejected()
    {
        var corrupt = TestFiles.Docx()[..40];

        RejectionOf(FileKind.Requirements, ".docx", corrupt).ShouldContain("not a valid Word");
    }

    [Fact]
    public void Docx_MissingTheWordDocumentPart_IsRejected()
    {
        RejectionOf(FileKind.Requirements, ".docx", TestFiles.Package("[Content_Types].xml", "other.xml"))
            .ShouldContain("not a valid Word");
    }

    [Fact]
    public void Xlsx_ThatIsReallyADocx_IsRejected()
    {
        RejectionOf(FileKind.TeamTemplate, ".xlsx", TestFiles.Docx()).ShouldContain("not a valid Excel");
    }

    [Fact]
    public void Package_ContainingAMacroProject_IsRejected()
    {
        var withMacro = TestFiles.Package("word/document.xml", "word/vbaProject.bin");

        RejectionOf(FileKind.Requirements, ".docx", withMacro).ShouldContain("macros");
    }

    [Fact]
    public void Package_WithTooManyParts_IsRejected()
    {
        var names = Enumerable.Range(0, FileRules.MaxArchiveEntries + 1).Select(i => $"parts/{i}.xml")
            .Append("word/document.xml").ToArray();

        RejectionOf(FileKind.Requirements, ".docx", TestFiles.Package(names)).ShouldContain("too many parts");
    }

    [Fact]
    public void Txt_WithNullBytes_IsRejected()
    {
        RejectionOf(FileKind.Requirements, ".txt", [0x48, 0x00, 0x49]).ShouldContain("plain text");
    }

    [Fact]
    public void Txt_ThatIsNotUtf8_IsRejected()
    {
        RejectionOf(FileKind.Requirements, ".txt", [0xFF, 0xFE, 0xFA, 0x41]).ShouldContain("UTF-8");
    }

    [Fact]
    public void Txt_OfOnlyWhitespace_IsRejected()
    {
        RejectionOf(FileKind.Requirements, ".txt", Encoding.UTF8.GetBytes("  \n\t ")).ShouldContain("no text");
    }

    [Theory]
    [InlineData(FileKind.Requirements, ".xlsx")]
    [InlineData(FileKind.Requirements, ".exe")]
    [InlineData(FileKind.TeamTemplate, ".docx")]
    [InlineData(FileKind.TeamTemplate, ".txt")]
    public void ExtensionNotAllowedForTheKind_IsRejected(FileKind kind, string extension)
    {
        RejectionOf(kind, extension, TestFiles.Docx()).ShouldContain("accepted here");
    }
}
