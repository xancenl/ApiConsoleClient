namespace ApiConsoleClient.ApiClient;

/// <summary>
/// Provides authentication headers for HTTP requests.
/// </summary>
internal interface IAuthHeaderProvider
{
    /// <summary>Returns authentication headers to include on each request.</summary>
    IReadOnlyDictionary<string, string> GetAuthHeaders();
}

/// <summary>
/// Basic authentication provider reading USERNAME and PASSWORD from environment variables.
/// If either value is missing, no Authorization header is emitted.
/// </summary>
internal sealed class BasicAuthHeaderProvider : IAuthHeaderProvider
{
    public IReadOnlyDictionary<string, string> GetAuthHeaders()
    {
        var user = Environment.GetEnvironmentVariable("USERNAME") ?? Environment.GetEnvironmentVariable("API_USERNAME");
        var pass = Environment.GetEnvironmentVariable("PASSWORD") ?? Environment.GetEnvironmentVariable("API_PASSWORD");
        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
        {
            // Provide a single-use warning header marker we can detect (or simply no header) – here we log once.
            if (!BasicAuthHeaderProviderWarning.HasWarned)
            {
                BasicAuthHeaderProviderWarning.HasWarned = true;
                Console.Error.WriteLine("[Auth] Basic auth credentials missing. Set USERNAME & PASSWORD (or API_USERNAME & API_PASSWORD) environment variables. No Authorization header will be sent.");
            }
            return Array.Empty<KeyValuePair<string,string>>().ToDictionary(k => k.Key, v => v.Value); // empty
        }

        var raw = System.Text.Encoding.UTF8.GetBytes(user + ":" + pass);
        var b64 = Convert.ToBase64String(raw);
        return new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Authorization"] = $"Basic {b64}"
        };
    }
}

internal static class BasicAuthHeaderProviderWarning
{
    internal static bool HasWarned;
}
