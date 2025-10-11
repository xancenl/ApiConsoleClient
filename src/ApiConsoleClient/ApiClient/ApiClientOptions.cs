using System.Text.Json;

namespace ApiConsoleClient.ApiClient;

/// <summary>
/// Options for configuring the ApiClient.
/// </summary>
internal sealed class ApiClientOptions
{
    public required Uri BaseUrl { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(100);
    public string UserAgent { get; init; } = "ApiConsoleClient/1.0";
    public Dictionary<string, string> DefaultHeaders { get; init; } = new();
    /// <summary>
    /// When true, verbose mode appends a `_reqId` query parameter with the correlation id. Defaults to false.
    /// </summary>
    public bool IncludeRequestIdAsQuery { get; init; }

    /// <summary>
    /// Optional telemetry sink to receive request completion events (success and error) with timing.
    /// </summary>
    public IApiTelemetrySink? TelemetrySink { get; init; }
}

internal static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}
