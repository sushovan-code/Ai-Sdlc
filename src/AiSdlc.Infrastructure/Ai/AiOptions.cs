namespace AiSdlc.Infrastructure.Ai;

/// <summary>Bound to the <c>Ai</c> section. The API key comes from user secrets or the environment, never from a file.</summary>
public sealed class AiOptions
{
    public const string SectionName = "Ai";
    public const string FakeProvider = "Fake";
    public const string OpenAiProvider = "OpenAI";
    public const string DefaultModel = "gpt-5.4-mini";
    public const int DefaultTimeoutSeconds = 120;

    /// <summary><c>Fake</c> answers offline and deterministically (tests, demos); <c>OpenAI</c> calls the real model.</summary>
    public string Provider { get; set; } = FakeProvider;

    public string Model { get; set; } = DefaultModel;

    public string? ApiKey { get; set; }

    public int TimeoutSeconds { get; set; } = DefaultTimeoutSeconds;

    public bool IsValid() => Provider switch
    {
        FakeProvider => true,
        OpenAiProvider => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(Model) && TimeoutSeconds > 0,
        _ => false,
    };
}
