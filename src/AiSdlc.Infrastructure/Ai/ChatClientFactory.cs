using System.ClientModel.Primitives;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;

namespace AiSdlc.Infrastructure.Ai;

/// <summary>Factory pattern: picks the <see cref="IChatClient"/> adapter named by <c>Ai:Provider</c>.</summary>
internal static class ChatClientFactory
{
    public static IChatClient Create(IOptions<AiOptions> configured)
    {
        var options = configured.Value;
        return options.Provider == AiOptions.OpenAiProvider ? CreateOpenAi(options) : new FakeChatClient();
    }

    private static IChatClient CreateOpenAi(AiOptions options)
    {
        var client = new OpenAIClient(
            new System.ClientModel.ApiKeyCredential(options.ApiKey!),
            new OpenAIClientOptions { NetworkTimeout = TimeSpan.FromSeconds(options.TimeoutSeconds) });
        return client.GetChatClient(options.Model).AsIChatClient();
    }
}
