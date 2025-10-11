namespace ApiConsoleClient.ApiClient;

/// <summary>
/// Minimal telemetry sink for receiving per-request metrics.
/// </summary>
internal interface IApiTelemetrySink
{
    void OnRequestCompleted(RequestCompleted evt);
    void OnRequestFailed(RequestFailed evt);
}

internal sealed class RequestCompleted
{
    public required string RequestId { get; init; }
    public required string Method { get; init; }
    public required string Url { get; init; }
    public required int Status { get; init; }
    public required long ElapsedMilliseconds { get; init; }
    public long? ContentLength { get; init; }
}

internal sealed class RequestFailed
{
    public required string RequestId { get; init; }
    public required string Method { get; init; }
    public required string Url { get; init; }
    public required int Status { get; init; }
    public required long ElapsedMilliseconds { get; init; }
    public Exception? Exception { get; init; }
}
