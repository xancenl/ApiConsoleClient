using System.Text.Json;
using ApiConsoleClient.ApiClient;
using Microsoft.Extensions.Logging;

namespace ApiConsoleClient;

/// <summary>
/// Application entry that parses CLI arguments and invokes API operations.
/// </summary>
internal sealed class App
{
    private readonly ILogger<App> _logger;
    private readonly IApiClient _client;

    /// <summary>
    /// Creates an App instance.
    /// </summary>
    public App(ILogger<App> logger, IApiClient client)
    {
        _logger = logger;
        _client = client;
    }

    /// <summary>
    /// Runs the app with CLI args.
    /// </summary>
    public async Task<int> RunAsync(string[] args)
    {
    var (operation, inputJson, dump, verbose) = ParseArgs(args);
    VerboseSwitch.IsVerbose = verbose;

        if (string.IsNullOrWhiteSpace(operation))
        {
            var ops = _client.ListOperationIds().OrderBy(x => x).ToArray();
            Console.WriteLine("Beschikbare operationIds:");
            foreach (var op in ops)
                Console.WriteLine(" - " + op);
            return 0;
        }

        try
        {
            var result = await _client.ExecuteAsync(operation!, inputJson).ConfigureAwait(false);
            if (dump)
            {
                var json = JsonSerializer.Serialize(result, ApiConsoleClient.ApiClient.JsonOptions.Default);
                Console.WriteLine(json);
            }
            else
            {
                Console.WriteLine("OK");
            }
            return 0;
        }
        catch (ApiException ex)
        {
            Log.ApiError(_logger, ex, ex.StatusCode, ex.ReasonPhrase);
            await Console.Error.WriteLineAsync(ex.ToDetailedString()).ConfigureAwait(false);
            return 1;
        }
    }

    private static (string? operation, string? inputJson, bool dump, bool verbose) ParseArgs(string[] args)
    {
        string? operation = null;
        string? input = null;
        bool dump = false;
        bool verbose = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--operation":
                    operation = i + 1 < args.Length ? args[++i] : null;
                    break;
                case "--input":
                    input = i + 1 < args.Length ? args[++i] : null;
                    break;
                case "--dump": dump = true; break;
                case "--verbose": verbose = true; break;
                case "-v": verbose = true; break;
            }
        }
        return (operation, input, dump, verbose);
    }
}

internal static class VerboseSwitch
{
    internal static bool IsVerbose;
}

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Error, Message = "API error {Status}: {Reason}")]
    public static partial void ApiError(ILogger logger, Exception exception, int Status, string Reason);
}
