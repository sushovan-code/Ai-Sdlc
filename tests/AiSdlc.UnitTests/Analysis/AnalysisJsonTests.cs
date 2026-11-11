using AiSdlc.Application.Analysis.Agents;
using AiSdlc.UnitTests.Support;
using Shouldly;

namespace AiSdlc.UnitTests.Analysis;

public sealed class AnalysisJsonTests
{
    [Fact]
    public void ParseDraft_ValidAnswer_IsNormalised()
    {
        var (content, problem) = AnalysisJson.ParseDraft(AnalysisSamples.DraftJson);

        problem.ShouldBeNull();
        content!.Summary.ShouldBe("Pay with a saved card.");
        content.FunctionalRequirements.Single().Priority.ShouldBe("High");
    }

    [Fact]
    public void ParseDraft_AnswerWrappedInACodeFence_IsStillRead()
    {
        var (content, _) = AnalysisJson.ParseDraft($"```json\n{AnalysisSamples.DraftJson}\n```");

        content.ShouldNotBeNull();
    }

    [Fact]
    public void ParseDraft_UnknownPriority_BecomesMedium()
    {
        var json = AnalysisSamples.DraftJson.Replace("\"high\"", "\"urgent\"", StringComparison.Ordinal);

        AnalysisJson.ParseDraft(json).Content!.FunctionalRequirements.Single().Priority.ShouldBe("Medium");
    }

    [Theory]
    [InlineData("not json at all", "not valid JSON")]
    [InlineData("""{"summary":"","functionalRequirements":[]}""", "summary is missing")]
    [InlineData("""{"summary":"x","functionalRequirements":[]}""", "no functional requirements")]
    [InlineData(
        """{"summary":"x","functionalRequirements":[{"id":"FR-1","title":"a","description":"b"},{"id":"fr-1","title":"c","description":"d"}]}""",
        "ids must be present and unique")]
    public void ParseDraft_UnusableAnswer_ReportsTheProblem(string answer, string expected)
    {
        var (content, problem) = AnalysisJson.ParseDraft(answer);

        content.ShouldBeNull();
        problem.ShouldNotBeNull().ShouldContain(expected);
    }

    [Fact]
    public void ParseCriticNotes_KeepsAtMostEightNonBlankNotes()
    {
        var notes = Enumerable.Range(1, 12).Select(i => new { severity = "Low", note = $"n{i}" })
            .Append(new { severity = "Low", note = "  " });

        var parsed = AnalysisJson.ParseCriticNotes(AnalysisJson.Serialize(new { notes }));

        parsed.Count.ShouldBe(8);
    }
}
