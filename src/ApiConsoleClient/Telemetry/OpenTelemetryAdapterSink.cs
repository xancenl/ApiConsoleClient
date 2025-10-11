using System.Diagnostics;
using ApiConsoleClient.ApiClient;

namespace ApiConsoleClient.Telemetry;

/// <summary>
/// Emits basic Activities for requests; can be hooked by OpenTelemetry if configured in the host.
/// </summary>
internal sealed class OpenTelemetryAdapterSink : IApiTelemetrySink
{
    private static readonly ActivitySource Source = new("ApiConsoleClient");

    public void OnRequestCompleted(RequestCompleted evt)
    {
        using var activity = Source.StartActivity("http.client", ActivityKind.Client);
        if (activity is null) return;
        activity.SetTag("http.request_id", evt.RequestId);
        activity.SetTag("http.method", evt.Method);
        activity.SetTag("url.full", evt.Url);
        activity.SetTag("http.status_code", evt.Status);
        activity.SetTag("http.response_content_length", evt.ContentLength);
        activity.SetTag("http.elapsed_ms", evt.ElapsedMilliseconds);
    }

    public void OnRequestFailed(RequestFailed evt)
    {
        using var activity = Source.StartActivity("http.client", ActivityKind.Client);
        if (activity is null) return;
        activity.SetTag("http.request_id", evt.RequestId);
        activity.SetTag("http.method", evt.Method);
        activity.SetTag("url.full", evt.Url);
        activity.SetTag("http.status_code", evt.Status);
        activity.SetTag("error", true);
        activity.SetTag("exception.type", evt.Exception?.GetType().FullName);
        activity.SetTag("exception.message", evt.Exception?.Message);
        activity.SetTag("http.elapsed_ms", evt.ElapsedMilliseconds);
    }
}
