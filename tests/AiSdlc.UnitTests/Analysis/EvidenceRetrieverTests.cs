using AiSdlc.Application.Analysis.Agents;
using AiSdlc.Application.Analysis.Content;
using Shouldly;

namespace AiSdlc.UnitTests.Analysis;

public sealed class EvidenceRetrieverTests
{
    private static readonly SourceChunk[] Chunks =
    [
        new("C1", "Customers can pay by card."),
        new("C2", "Refunds are possible."),
        new("C10", new string('x', 400)),
    ];

    private static AnalysisContent Draft(params RequirementItem[] functional) =>
        new("Summary", functional, [], [], [], [], [], []);

    private static RequirementItem Requirement(string id, params string[] refs) =>
        new(id, "Title", "Description", "High", refs);

    [Fact]
    public void Attach_CitedChunks_BecomeQuotedEvidenceInDocumentOrder()
    {
        var result = EvidenceRetriever.Attach(Draft(Requirement("FR-1", "c10", "C2")), Chunks);

        result.Content.Evidence.Select(e => e.Ref).ShouldBe(["C2", "C10"]);
        result.Content.Evidence[0].Quote.ShouldBe("Refunds are possible.");
        result.Content.FunctionalRequirements[0].SourceRefs.ShouldBe(["C10", "C2"]);
        result.UnsupportedItemIds.ShouldBeEmpty();
    }

    [Fact]
    public void Attach_LongChunk_IsQuotedInShortenedForm()
    {
        var result = EvidenceRetriever.Attach(Draft(Requirement("FR-1", "C10")), Chunks);

        var quote = result.Content.Evidence.Single().Quote;
        quote.Length.ShouldBeLessThanOrEqualTo(EvidenceRetriever.MaxQuoteCharacters + 1);
        quote.ShouldEndWith("…");
    }

    [Fact]
    public void Attach_ACitationToAChunkThatDoesNotExist_IsDroppedAndTheRequirementReportedUnsupported()
    {
        var result = EvidenceRetriever.Attach(Draft(Requirement("FR-1", "C99")), Chunks);

        result.Content.FunctionalRequirements[0].SourceRefs.ShouldBeEmpty();
        result.Content.Evidence.ShouldBeEmpty();
        result.UnsupportedItemIds.ShouldBe(["FR-1"]);
    }

    [Fact]
    public void Attach_AGapWithoutCitations_IsNotReportedBecauseGapsAreAboutWhatIsMissing()
    {
        var draft = Draft(Requirement("FR-1", "C1")) with { Gaps = [new QuestionItem("GAP-1", "No limits stated.", [])] };

        EvidenceRetriever.Attach(draft, Chunks).UnsupportedItemIds.ShouldBeEmpty();
    }
}
