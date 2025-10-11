using System.Text.Json.Nodes;

namespace ApiConsoleClient.ApiClient;

/// <summary>
/// Dynamic API client capable of listing operations by id and executing an operation with a JSON input payload.
/// </summary>
/// <summary>
/// Internal interface used by the console app to execute operations discovered from the OpenAPI.
/// </summary>
internal interface IApiClient
{
    /// <summary>Returns all available operationIds.</summary>
    IEnumerable<string> ListOperationIds();

    /// <summary>Executes an operation by operationId with optional JSON input mapping to parameters/body.</summary>
    Task<object?> ExecuteAsync(string operationId, string? inputJson, CancellationToken cancellationToken = default);
}
