using AiSdlc.Application.Files;
using AiSdlc.Domain.Projects;
using Shouldly;

namespace AiSdlc.UnitTests.Files;

public sealed class FileRulesTests
{
    [Theory]
    [InlineData(FileKind.Requirements, ".docx", true)]
    [InlineData(FileKind.Requirements, ".TXT", true)]
    [InlineData(FileKind.Requirements, ".xlsx", false)]
    [InlineData(FileKind.Requirements, ".docm", false)]
    [InlineData(FileKind.TeamTemplate, ".xlsx", true)]
    [InlineData(FileKind.TeamTemplate, ".xlsm", false)]
    [InlineData(FileKind.TeamTemplate, ".csv", false)]
    public void IsAllowed_ChecksTheAllowList(FileKind kind, string extension, bool expected)
    {
        FileRules.IsAllowed(kind, extension).ShouldBe(expected);
    }

    [Theory]
    [InlineData(".docx", FileRules.DocxContentType)]
    [InlineData(".xlsx", FileRules.XlsxContentType)]
    [InlineData(".txt", FileRules.TextContentType)]
    public void ContentTypeFor_IsDecidedByTheServerFromTheExtension(string extension, string expected)
    {
        FileRules.ContentTypeFor(extension).ShouldBe(expected);
    }

    [Fact]
    public void MaxFileBytes_IsTenMegabytes()
    {
        FileRules.MaxFileBytes.ShouldBe(10 * 1024 * 1024);
    }
}
