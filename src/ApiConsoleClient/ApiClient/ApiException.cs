namespace ApiConsoleClient.ApiClient;

/// <summary>
/// Rich API exception that contains HTTP and problem details.
/// </summary>
internal sealed class ApiException : Exception
{
    public int StatusCode { get; }
    public string ReasonPhrase { get; } = string.Empty;
    public string ResponseBody { get; } = string.Empty;
    public object? ProblemDetails { get; }
    public string Method { get; } = string.Empty;
    public Uri Url { get; } = new("http://localhost/");

    public ApiException(int statusCode, string reasonPhrase, string responseBody, object? problemDetails, string method, Uri url)
        : base($"HTTP {statusCode} {reasonPhrase}")
    {
        StatusCode = statusCode;
        ReasonPhrase = reasonPhrase;
        ResponseBody = responseBody;
        ProblemDetails = problemDetails;
        Method = method;
        Url = url;
    }

    public ApiException() { }
    public ApiException(string? message) : base(message) { }
    public ApiException(string? message, Exception? innerException) : base(message, innerException) { }

    public string ToDetailedString()
    {
        return $"Status: {StatusCode} ({ReasonPhrase})\nMethod: {Method}\nUrl: {Url}\nBody: {ResponseBody}";
    }
}
