using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Analysis.Content;
using AiSdlc.Application.Common;
using AiSdlc.Domain.Auditing;

namespace AiSdlc.Application.Analysis.Agents;

/// <summary>
/// Coordinates the agents for one analysis: the governance guard masks sensitive data, the specialist drafts, the
/// retrieval agent attaches evidence, the critic reviews and the guard flags unsupported claims. Every model call is
/// audited. Whether analysis may run at all (the approval gates) is decided before this is called.
/// </summary>
public sealed class AnalysisOrchestrator(
    AnalysisSpecialist specialist,
    AnalysisCritic critic,
    ICurrentUser currentUser,
    IAuditLog audit)
{
    private const string SpecialistAgent = "analysis-specialist";
    private const string CriticAgent = "analysis-critic";

    public async Task<Result<AnalysisResult>> RunAsync(
        string requirementsText,
        AnalysisContent? previous,
        string? feedback,
        CancellationToken cancellationToken)
    {
        var redacted = GovernanceGuard.Redact(requirementsText);
        var chunks = RequirementsChunker.Split(redacted.Text);

        // Reviewer comments are free text too: mask sensitive data in them before they reach the model.
        var safeFeedback = feedback is null ? null : GovernanceGuard.Redact(feedback).Text;
        var drafted = await specialist.DraftAsync(chunks, previous, safeFeedback, cancellationToken);
        await AuditCallAsync(SpecialistAgent, drafted.IsSuccess, drafted.IsSuccess ? drafted.Value.Model : string.Empty, drafted.IsSuccess ? drafted.Value.TotalTokens : null, cancellationToken);
        if (drafted.IsFailure)
        {
            return Result<AnalysisResult>.Failure(drafted.Error);
        }

        var withEvidence = EvidenceRetriever.Attach(drafted.Value.Content, chunks);
        var notes = await critic.ReviewAsync(chunks, withEvidence.Content, cancellationToken);
        await AuditCallAsync(CriticAgent, !notes.Any(n => n.Note == AnalysisCritic.UnavailableNote), drafted.Value.Model, null, cancellationToken);

        var flags = redacted.Flags.Concat(GovernanceGuard.CheckSupport(withEvidence.UnsupportedItemIds)).ToList();
        return Result<AnalysisResult>.Success(
            new AnalysisResult(withEvidence.Content, new AnalysisNotes(notes, flags), drafted.Value.Model));
    }

    private Task AuditCallAsync(string agent, bool succeeded, string model, long? tokens, CancellationToken cancellationToken)
    {
        var details = $"agent={agent} model={(model.Length == 0 ? "n/a" : model)} prompt={AnalysisPrompts.Version}" +
                      (tokens is null ? string.Empty : $" tokens={tokens}");
        return audit.WriteAsync(
            new AuditRecord(
                AuditActions.AiCall,
                succeeded ? AuditOutcome.Success : AuditOutcome.Failure,
                currentUser.UserId,
                currentUser.Email,
                details),
            cancellationToken);
    }
}
