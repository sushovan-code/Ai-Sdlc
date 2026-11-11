using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Analysis;
using AiSdlc.Application.Analysis.Agents;
using AiSdlc.Application.Analysis.Content;
using AiSdlc.Application.Common;
using AiSdlc.Application.Stages.Content;
using AiSdlc.Domain.Artifacts;
using AiSdlc.Domain.Auditing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AiSdlc.Application.Stages.Agents;

/// <summary>
/// Drafts the architecture, the tasks or the test cases from the earlier approved output. Like the analysis agents it
/// masks sensitive data in reviewer feedback, caps output size, checks the answer against the schema, retries once,
/// drops citations to ids that do not exist and flags items nothing traces back to. Tasks are placed by the scheduler,
/// not the model. Every model call is audited.
/// </summary>
public sealed partial class StageGenerator(
    IChatClient chat,
    ICurrentUser currentUser,
    IAuditLog audit,
    ILogger<StageGenerator> logger)
{
    private const int MaxAttempts = 2;
    private const string AgentPrefix = "stage-";

    public async Task<Result<StageDraft>> GenerateAsync(
        Guid projectId,
        ArtifactStage stage,
        StageSource source,
        StageContent? previous,
        string? feedback,
        CancellationToken cancellationToken)
    {
        var safeFeedback = feedback is null ? null : GovernanceGuard.Redact(feedback).Text;
        var previousJson = previous is null ? null : AnalysisJson.Serialize(previous);
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, StagePrompts.SystemFor(stage)),
            new(ChatRole.User, StagePrompts.UserFor(source, previousJson, safeFeedback)),
        };
        var options = new ChatOptions { MaxOutputTokens = AiLimits.MaxOutputTokens, ResponseFormat = ChatResponseFormat.Json };

        string? problem = null;
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            ChatResponse response;
            try
            {
                response = await chat.GetResponseAsync(messages, options, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                LogCallFailed(exception, stage);
                await AuditCallAsync(projectId, stage, false, string.Empty, null, cancellationToken);
                return Result<StageDraft>.Failure(AnalysisErrors.AiUnavailable);
            }

            var model = response.ModelId ?? chat.GetService<ChatClientMetadata>()?.DefaultModelId ?? "unknown";
            var built = Build(stage, response.Text, source);
            if (built.Draft is not null)
            {
                await AuditCallAsync(projectId, stage, true, model, response.Usage?.TotalTokenCount, cancellationToken);
                return Result<StageDraft>.Success(new StageDraft(built.Draft.Content, built.Draft.Notes, model));
            }

            problem = built.Problem;
            messages.Add(new ChatMessage(ChatRole.Assistant, response.Text));
            messages.Add(new ChatMessage(ChatRole.User, StagePrompts.RetryUser(problem!)));
        }

        LogInvalidOutput(stage, problem);
        await AuditCallAsync(projectId, stage, false, string.Empty, null, cancellationToken);
        return Result<StageDraft>.Failure(AnalysisErrors.AiInvalidOutput);
    }

    private static (StageDraft? Draft, string? Problem) Build(ArtifactStage stage, string text, StageSource source)
    {
        if (stage == ArtifactStage.Tasks)
        {
            return BuildTasks(text, source);
        }

        var (parsed, problem) = StageJson.ParseSections(text);
        if (parsed is null)
        {
            return (null, problem);
        }

        var known = source.KnownRefs;
        var sections = parsed.Sections
            .Select(s => new StageSection(s.Title, s.Items.Select(i => i with { Refs = KnownOnly(i.Refs, known) }).ToList()))
            .ToList();
        var untraced = sections.SelectMany(s => s.Items).Where(i => i.Refs.Count == 0).Select(i => i.Id).ToList();
        var content = parsed with { Sections = sections };
        return (new StageDraft(content, new StageNotes([], Flags(untraced), []), string.Empty), null);
    }

    private static (StageDraft? Draft, string? Problem) BuildTasks(string text, StageSource source)
    {
        var (summary, tasks, problem) = StageJson.ParseTasks(text);
        if (tasks is null)
        {
            return (null, problem);
        }

        var cleaned = tasks.Select(t => t with { Refs = KnownOnly(t.Refs, source.KnownRefs) }).ToList();
        var (items, assignments) = TaskPlanner.Plan(cleaned, source.Team, source.SprintWeeks);
        var content = new StageContent(summary!, [new StageSection("Tasks", items)]);
        var untraced = items.Where(i => i.Refs.Count == 0).Select(i => i.Id).ToList();
        return (new StageDraft(content, new StageNotes([], Flags(untraced), assignments), string.Empty), null);
    }

    private static List<string> KnownOnly(IReadOnlyList<string> refs, IReadOnlySet<string> known) =>
        refs.Where(known.Contains).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    private static List<GuardrailFlag> Flags(List<string> untraced) =>
        untraced.Count == 0
            ? []
            : [new GuardrailFlag(
                GovernanceGuard.UnsupportedCode,
                $"Nothing earlier in the project is cited for: {string.Join(", ", untraced)}. Check these before approving.")];

    private Task AuditCallAsync(
        Guid projectId, ArtifactStage stage, bool succeeded, string model, long? tokens, CancellationToken cancellationToken)
    {
        var details = $"{projectId}: agent={AgentPrefix}{stage.ToString().ToLowerInvariant()} model={(model.Length == 0 ? "n/a" : model)} prompt={StagePrompts.Version}" +
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

    [LoggerMessage(Level = LogLevel.Warning, Message = "The AI model call for the {Stage} stage failed.")]
    private partial void LogCallFailed(Exception exception, ArtifactStage stage);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The AI model gave unusable output for the {Stage} stage twice: {Problem}")]
    private partial void LogInvalidOutput(ArtifactStage stage, string? problem);
}
