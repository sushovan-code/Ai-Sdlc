using AiSdlc.Application.Stages.Content;

namespace AiSdlc.Application.Stages.Agents;

/// <summary>What the stage generator produced: the content, its notes and the model that wrote it.</summary>
public sealed record StageDraft(StageContent Content, StageNotes Notes, string Model);
