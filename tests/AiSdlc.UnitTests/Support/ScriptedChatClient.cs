using Microsoft.Extensions.AI;

namespace AiSdlc.UnitTests.Support;

/// <summary>A chat client that replays prepared answers (text, or an exception to throw) and remembers what it was asked.</summary>
public sealed class ScriptedChatClient(params object[] answers) : IChatClient
{
    private readonly Queue<object> _answers = new(answers);

    /// <summary>The user message of every call, in order.</summary>
    public List<string> UserPrompts { get; } = [];

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        UserPrompts.Add(messages.Last(m => m.Role == ChatRole.User).Text);
        var next = _answers.Count > 0 ? _answers.Dequeue() : throw new InvalidOperationException("No scripted answer left.");
        return next is Exception exception
            ? throw exception
            : Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, (string)next)) { ModelId = "scripted-model" });
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
