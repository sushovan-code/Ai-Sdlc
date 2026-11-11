using AiSdlc.Application.Analysis.Agents;
using Shouldly;

namespace AiSdlc.UnitTests.Analysis;

public sealed class RequirementsChunkerTests
{
    [Fact]
    public void Split_ShortText_IsOneChunkNumberedC1()
    {
        var chunks = RequirementsChunker.Split("First line.\r\nSecond line.");

        var chunk = chunks.ShouldHaveSingleItem();
        chunk.Id.ShouldBe("C1");
        chunk.Text.ShouldBe("First line.\nSecond line.");
    }

    [Fact]
    public void Split_ManyParagraphs_StartsANewChunkWhenTheTargetSizeIsReached()
    {
        var paragraph = new string('a', 500);

        var chunks = RequirementsChunker.Split(string.Join('\n', paragraph, paragraph, paragraph));

        chunks.Select(c => c.Id).ShouldBe(["C1", "C2"]);
        chunks.ShouldAllBe(c => c.Text.Length <= RequirementsChunker.TargetCharacters);
    }

    [Fact]
    public void Split_OneEnormousParagraph_IsCutAtSpacesSoNoChunkIsHuge()
    {
        var text = string.Join(' ', Enumerable.Repeat("word", 1000));

        var chunks = RequirementsChunker.Split(text);

        chunks.Count.ShouldBeGreaterThan(1);
        chunks.ShouldAllBe(c => c.Text.Length <= RequirementsChunker.TargetCharacters);
        string.Join(' ', chunks.Select(c => c.Text.Replace('\n', ' '))).ShouldBe(text);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  \n \r\n ")]
    public void Split_NoText_GivesNoChunks(string text)
    {
        RequirementsChunker.Split(text).ShouldBeEmpty();
    }
}
