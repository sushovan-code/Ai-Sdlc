using AiSdlc.Application.Files;
using Shouldly;

namespace AiSdlc.UnitTests.Files;

public sealed class FileNamesTests
{
    [Theory]
    [InlineData("requirements.docx", "requirements.docx")]
    [InlineData("../../etc/passwd.txt", "passwd.txt")]
    [InlineData("..\\..\\windows\\evil.docx", "evil.docx")]
    [InlineData("C:\\temp\\team.xlsx", "team.xlsx")]
    [InlineData("na<me>:with|bad?chars*.txt", "namewithbadchars.txt")]
    [InlineData("tab\tand\nnewline.txt", "tabandnewline.txt")]
    [InlineData("  spaced name.docx  ", "spaced name.docx")]
    [InlineData("...hidden.txt", "hidden.txt")]
    public void Sanitize_RemovesPathsAndUnsafeCharacters(string input, string expected)
    {
        FileNames.Sanitize(input).ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("../")]
    [InlineData("...")]
    public void Sanitize_NothingUsable_FallsBackToAGenericName(string? input)
    {
        FileNames.Sanitize(input).ShouldBe("file");
    }

    [Fact]
    public void Sanitize_VeryLongName_IsShortenedButKeepsItsExtension()
    {
        var name = new string('a', 400) + ".docx";

        var result = FileNames.Sanitize(name);

        result.Length.ShouldBe(FileRules.MaxFileNameLength);
        result.ShouldEndWith(".docx");
    }

    [Theory]
    [InlineData("a.DOCX", ".docx")]
    [InlineData("a.tar.gz", ".gz")]
    [InlineData("noextension", "")]
    public void ExtensionOf_IsLowerCaseAndLastSegmentOnly(string name, string expected)
    {
        FileNames.ExtensionOf(name).ShouldBe(expected);
    }
}
