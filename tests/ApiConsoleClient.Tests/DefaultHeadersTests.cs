using System.Net.Http;
using ApiConsoleClient.ApiClient;
using Flurl.Http.Testing;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApiConsoleClient.Tests;

public class DefaultHeadersTests
{
    [Fact]
    public async Task NonVerboseAddsDefaultHeadersAndUserAgent()
    {
        using var http = new HttpTest();
        http.RespondWithJson(new { ok = true });
        var options = new ApiClientOptions
        {
            BaseUrl = new Uri("https://api.test"),
            DefaultHeaders = new Dictionary<string, string> { ["X-Custom"] = "abc" },
            UserAgent = "ApiConsoleClient.Tests/1.0"
        };
        var client = new ApiClient.ApiClient(new NullLogger<ApiClient.ApiClient>(), options, new NoAuth());
        VerboseSwitch.IsVerbose = false;

        await client.GetHealthAsync(new GetHealthRequest());

        http.ShouldHaveCalled("https://api.test/health")
            .WithVerb(HttpMethod.Get)
            .WithHeader("User-Agent", "ApiConsoleClient.Tests/1.0")
            .WithHeader("X-Custom", "abc");
    }
}
