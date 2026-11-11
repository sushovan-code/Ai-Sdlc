namespace AiSdlc.Application.Analysis;

/// <summary>Caps that keep one request from costing too much or running too long.</summary>
public static class AiLimits
{
    /// <summary>The most text from the requirements that is sent to the model in one pass.</summary>
    public const int MaxSourceCharacters = 60_000;

    public const int MaxOutputTokens = 6_000;

    public const int CriticMaxOutputTokens = 1_500;

    public const int MaxFeedbackLength = 2_000;
}
