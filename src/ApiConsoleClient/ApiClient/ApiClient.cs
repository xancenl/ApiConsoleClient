using System.Text;
using System.Diagnostics;
using System.Text.Json;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Logging;

namespace ApiConsoleClient.ApiClient;

/// <summary>
/// Flurl-based API client. The generated code augments this class via partials to provide per-endpoint methods.
/// </summary>
internal partial class ApiClient : IApiClient
{
    private readonly ILogger<ApiClient> _logger;
    private readonly ApiClientOptions _options;
    private readonly IAuthHeaderProvider _auth;

    // Precompiled logging delegates for high-volume structured HTTP instrumentation (verbose mode only)
    private static readonly Action<ILogger, string, string?, string?, Exception?> LogRequestBegin =
        LoggerMessage.Define<string, string?, string?>(
            LogLevel.Information,
            new EventId(1000, nameof(LogRequestBegin)),
            "HTTP {RequestId} BEGIN {Method} {Url}");

    private static readonly Action<ILogger, string, int?, string?, string?, long, long?, Exception?> LogRequestEnd =
        LoggerMessage.Define<string, int?, string?, string?, long, long?>(
            LogLevel.Information,
            new EventId(1001, nameof(LogRequestEnd)),
            "HTTP {RequestId} END {Status} {Method} {Url} in {ElapsedMs} ms len={ContentLength}");

    private static readonly Action<ILogger, string, int?, string?, string?, long, Exception?> LogRequestError =
        LoggerMessage.Define<string, int?, string?, string?, long>(
            LogLevel.Error,
            new EventId(1002, nameof(LogRequestError)),
            "HTTP {RequestId} ERR {Status} {Method} {Url} after {ElapsedMs} ms");

    /// <summary>Creates a new ApiClient.</summary>
    public ApiClient(ILogger<ApiClient> logger, ApiClientOptions options, IAuthHeaderProvider auth)
    {
        _logger = logger;
        _options = options;
        _auth = auth;
    }

    /// <inheritdoc />
    public IEnumerable<string> ListOperationIds() => GeneratedListOperationIds();

    /// <inheritdoc />
    public async Task<object?> ExecuteAsync(string operationId, string? inputJson, CancellationToken cancellationToken = default)
    {
        return await GeneratedExecuteAsync(operationId, inputJson, cancellationToken).ConfigureAwait(false);
    }

    private IFlurlRequest CreateRequest(string path)
    {
        var url = new Url(_options.BaseUrl).AppendPathSegment(path.TrimStart('/'), fullyEncode: false);
        return CreateRequest(url);
    }

    private IFlurlRequest CreateRequest(Url url)
    {
        if (VerboseSwitch.IsVerbose)
        {
            var requestId = Guid.NewGuid().ToString("n");
            var sw = Stopwatch.StartNew();
            var localUrl = _options.IncludeRequestIdAsQuery ? url.SetQueryParam("_reqId", requestId) : url;
            var instrumented = new FlurlRequest(localUrl)
                .BeforeCall(call =>
                {
                    call.Request.Headers.Add("X-Request-Id", requestId);
                    LogRequestBegin(_logger, requestId, call.Request.Verb?.Method, call.Request.Url, null);
                })
                .AfterCall(call =>
                {
                    sw.Stop();
                    var len = call.Response?.ResponseMessage?.Content?.Headers?.ContentLength;
                    var status = (int?)call.Response?.StatusCode;
                    LogRequestEnd(_logger, requestId, status, call.Request.Verb?.Method, call.Request.Url, sw.ElapsedMilliseconds, len, null);
                    _options.TelemetrySink?.OnRequestCompleted(new RequestCompleted
                    {
                        RequestId = requestId,
                        Method = call.Request.Verb?.Method ?? "",
                        Url = call.Request.Url?.ToString() ?? string.Empty,
                        Status = status ?? 0,
                        ElapsedMilliseconds = sw.ElapsedMilliseconds,
                        ContentLength = len
                    });
                })
                .OnError(call =>
                {
                    if (sw.IsRunning) sw.Stop();
                    var status = (int?)call.Response?.StatusCode;
                    LogRequestError(_logger, requestId, status, call.Request.Verb?.Method, call.Request.Url, sw.ElapsedMilliseconds, call.Exception);
                    _options.TelemetrySink?.OnRequestFailed(new RequestFailed
                    {
                        RequestId = requestId,
                        Method = call.Request.Verb?.Method ?? "",
                        Url = call.Request.Url?.ToString() ?? string.Empty,
                        Status = status ?? 0,
                        ElapsedMilliseconds = sw.ElapsedMilliseconds,
                        Exception = call.Exception
                    });
                });
            instrumented = instrumented.WithTimeout(_options.Timeout);
            instrumented = instrumented.WithHeader("User-Agent", _options.UserAgent);
            foreach (var kv in _options.DefaultHeaders)
                instrumented = instrumented.WithHeader(kv.Key, kv.Value);
            foreach (var kv in _auth.GetAuthHeaders())
                instrumented = instrumented.WithHeader(kv.Key, kv.Value);
            return instrumented;
        }
        var req = url.WithTimeout(_options.Timeout);
        req = req.WithHeader("User-Agent", _options.UserAgent);
        foreach (var kv in _options.DefaultHeaders)
            req = req.WithHeader(kv.Key, kv.Value);
        foreach (var kv in _auth.GetAuthHeaders())
            req = req.WithHeader(kv.Key, kv.Value);
        // Attach lightweight telemetry even when not verbose
        if (_options.TelemetrySink is not null)
        {
            req = req
                .AfterCall(call =>
                {
                    var len = call.Response?.ResponseMessage?.Content?.Headers?.ContentLength;
                    var status = (int?)call.Response?.StatusCode ?? 0;
                    _options.TelemetrySink!.OnRequestCompleted(new RequestCompleted
                    {
                        RequestId = string.Empty,
                        Method = call.Request.Verb?.Method ?? "",
                        Url = call.Request.Url?.ToString() ?? string.Empty,
                        Status = status,
                        ElapsedMilliseconds = 0, // Not measured without verbose stopwatch
                        ContentLength = len
                    });
                })
                .OnError(call =>
                {
                    var status = (int?)call.Response?.StatusCode ?? 0;
                    _options.TelemetrySink!.OnRequestFailed(new RequestFailed
                    {
                        RequestId = string.Empty,
                        Method = call.Request.Verb?.Method ?? "",
                        Url = call.Request.Url?.ToString() ?? string.Empty,
                        Status = status,
                        ElapsedMilliseconds = 0,
                        Exception = call.Exception
                    });
                });
        }
        return req;
    }

    private static StringContent CreateJsonContent(object? body) =>
        new(JsonSerializer.Serialize(body, JsonOptions.Default), Encoding.UTF8, "application/json");

    private static async Task<T> ReadJsonAsync<T>(IFlurlResponse resp)
    {
        var s = await resp.ResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
        return JsonSerializer.Deserialize<T>(s, JsonOptions.Default)!;
    }

    private static async Task<ApiException> BuildApiExceptionAsync(FlurlHttpException ex)
    {
        var status = (int?)ex.Call?.Response?.StatusCode ?? 0;
        var reason = ex.Call?.Response?.ResponseMessage?.ReasonPhrase ?? ex.Message;
        var body = ex.Call is not null ? await ex.Call.Response.GetStringAsync().ConfigureAwait(false) : string.Empty;
        object? problem = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(body))
                problem = JsonSerializer.Deserialize<object>(body, JsonOptions.Default);
        }
        catch (JsonException)
        {
            // ignore parse errors
        }
        var method = ex.Call?.Request?.Verb.ToString() ?? "";
    var urlStr = ex.Call?.Request?.Url?.ToString() ?? string.Empty;
    var uri = Uri.TryCreate(urlStr, UriKind.Absolute, out var u) ? u! : new Uri("http://localhost/");
    return new ApiException(status, reason, body, problem, method, uri);
    }

    // The following partials are implemented by generated code
    private partial IEnumerable<string> GeneratedListOperationIds();
    private partial Task<object?> GeneratedExecuteAsync(string operationId, string? inputJson, CancellationToken cancellationToken);
}
