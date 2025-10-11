using ApiConsoleClient.ApiClient;

namespace ApiConsoleClient.Telemetry;

/// <summary>
/// Minimal telemetry sink that writes request metrics to the console.
/// </summary>
internal sealed class ConsoleTelemetrySink : IApiTelemetrySink
{
    public void OnRequestCompleted(RequestCompleted evt)
    {
        Console.WriteLine($"[http.completed] id={evt.RequestId} status={evt.Status} method={evt.Method} url={evt.Url} elapsedMs={evt.ElapsedMilliseconds} len={evt.ContentLength}");
    }

    public void OnRequestFailed(RequestFailed evt)
    {
        Console.Error.WriteLine($"[http.failed] id={evt.RequestId} status={evt.Status} method={evt.Method} url={evt.Url} elapsedMs={evt.ElapsedMilliseconds} error={evt.Exception?.Message}");
    }
}
