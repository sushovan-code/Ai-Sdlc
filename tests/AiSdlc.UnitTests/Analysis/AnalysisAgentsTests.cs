using AiSdlc.Application.Analysis;
using AiSdlc.Application.Analysis.Agents;
using AiSdlc.Application.Analysis.Content;
using AiSdlc.Application.Common;
using AiSdlc.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace AiSdlc.UnitTests.Analysis;

public sealed class AnalysisAgentsTests
{
    private static readonly SourceChunk[] Chunks = [new("C1", "Customers can pay by card.")];
    private static readonly AnalysisContent Draft = AnalysisJson.ParseDraft(AnalysisSamples.DraftJson).Content!;

    private static AnalysisSpecialist Specialist(ScriptedChatClient chat) => new(chat, NullLogger<AnalysisSpecialist>.Instance);

    private static AnalysisCritic Critic(ScriptedChatClient chat) => new(chat, NullLogger<AnalysisCritic>.Instance);

    [Fact]
    public async Task Specialist_ValidAnswer_ReturnsTheContentAndTheModelName()
    {
        var result = await Specialist(new ScriptedChatClient(AnalysisSamples.DraftJson))
            .DraftAsync(Chunks, null, null, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Model.ShouldBe("scripted-model");
        result.Value.Content.FunctionalRequirements.Single().Id.ShouldBe("FR-1");
    }

    [Fact]
    public async Task Specialist_FirstAnswerUnusable_RetriesOnceWithTheProblemDescribed()
    {
        var chat = new ScriptedChatClient("nonsense", AnalysisSamples.DraftJson);

        var result = await Specialist(chat).DraftAsync(Chunks, null, null, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        chat.UserPrompts.Count.ShouldBe(2);
        chat.UserPrompts[1].ShouldContain("could not be used");
    }

    [Fact]
    public async Task Specialist_BothAnswersUnusable_FailsWithInvalidOutput()
    {
        var result = await Specialist(new ScriptedChatClient("nonsense", "still nonsense"))
            .DraftAsync(Chunks, null, null, CancellationToken.None);

        result.Error.Code.ShouldBe(AnalysisErrors.AiInvalidOutputCode);
        result.Error.Type.ShouldBe(ErrorType.Unavailable);
    }

    [Fact]
    public async Task Specialist_TheModelCallThrows_FailsWithUnavailable()
    {
        var result = await Specialist(new ScriptedChatClient(new HttpRequestException("down")))
            .DraftAsync(Chunks, null, null, CancellationToken.None);

        result.Error.Code.ShouldBe(AnalysisErrors.AiUnavailableCode);
    }

    [Fact]
    public async Task Specialist_Cancelled_PropagatesTheCancellation()
    {
        var specialist = Specialist(new ScriptedChatClient(new OperationCanceledException()));

        await Should.ThrowAsync<OperationCanceledException>(
            () => specialist.DraftAsync(Chunks, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task Specialist_WithFeedback_PutsThePreviousDraftAndTheCommentInThePromptAsData()
    {
        var chat = new ScriptedChatClient(AnalysisSamples.DraftJson);

        await Specialist(chat).DraftAsync(Chunks, Draft, "Cover refunds", CancellationToken.None);

        var prompt = chat.UserPrompts.Single();
        prompt.ShouldContain("<previous_draft>");
        prompt.ShouldContain("<reviewer_feedback>");
        prompt.ShouldContain("Cover refunds");
    }

    [Fact]
    public async Task Critic_ReturnsTheNotes()
    {
        var notes = await Critic(new ScriptedChatClient(AnalysisSamples.CriticJson)).ReviewAsync(Chunks, Draft, CancellationToken.None);

        notes.ShouldHaveSingleItem().Note.ShouldBe("Refund rules are missing.");
    }

    [Fact]
    public async Task Critic_WhenTheModelFails_SaysTheReviewIsMissingInsteadOfFailingTheAnalysis()
    {
        var notes = await Critic(new ScriptedChatClient(new HttpRequestException("down")))
            .ReviewAsync(Chunks, Draft, CancellationToken.None);

        notes.ShouldHaveSingleItem().Note.ShouldBe(AnalysisCritic.UnavailableNote);
    }
}
