using System.Net.Http;
using ApiConsoleClient.ApiClient;
using Flurl.Http.Testing;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApiConsoleClient.Tests;

public class VerboseHeaderTests
{
    [Fact]
    public async Task SendsXRequestIdHeaderInVerboseMode()
    {
        using var http = new HttpTest();
        http.RespondWithJson(new { ok = true });
        var options = new ApiClientOptions { BaseUrl = new Uri("https://api.test"), IncludeRequestIdAsQuery = false };
        var client = new ApiClient.ApiClient(new NullLogger<ApiClient.ApiClient>(), options, new NoAuth());
        VerboseSwitch.IsVerbose = true;

        await client.GetHealthAsync(new GetHealthRequest());

        http.ShouldHaveCalled("https://api.test/health")
            .WithVerb(HttpMethod.Get)
            .WithHeader("X-Request-Id", "*");
    }
}
