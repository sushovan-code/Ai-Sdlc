using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiSdlc.IntegrationTests.Infrastructure;

/// <summary>Reads responses the way the API writes them: camelCase properties and enums as strings.</summary>
public static class ApiJson
{
    public static JsonSerializerOptions Options { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
