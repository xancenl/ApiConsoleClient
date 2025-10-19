using Flurl.Http.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using ApiConsoleClient.ApiClient;

namespace ApiConsoleClient.Tests;

public class VerboseLoggingTests
{
    [Fact]
    public async Task VerboseDoesNotAlterBaseUrlWhenQueryOptOut()
    {
        using var http = new HttpTest();
        http.RespondWithJson(new { ok = true });
        var options = new ApiClientOptions { BaseUrl = new Uri("https://api.test"), IncludeRequestIdAsQuery = false };
        var client = new ApiClient.ApiClient(new NullLogger<ApiClient.ApiClient>(), options, new NoAuth());
        VerboseSwitch.IsVerbose = true;
        await client.GetHealthAsync(new GetHealthRequest(), default);
        http.ShouldHaveCalled("https://api.test/health").WithVerb(HttpMethod.Get);
    }

    [Fact]
    public async Task VerboseAddsReqIdQueryWhenOptIn()
    {
        using var http = new HttpTest();
        http.RespondWithJson(new { ok = true });
        var options = new ApiClientOptions { BaseUrl = new Uri("https://api.test"), IncludeRequestIdAsQuery = true };
        var client = new ApiClient.ApiClient(new NullLogger<ApiClient.ApiClient>(), options, new NoAuth());
        VerboseSwitch.IsVerbose = true;
        await client.GetHealthAsync(new GetHealthRequest(), default);
        http.ShouldHaveCalled("https://api.test/health*")
            .WithVerb(HttpMethod.Get)
            .With(call => {
                if (call.Request.Url is null) return false;
                var u = new Uri(call.Request.Url);
                return u.Query.Contains("_reqId=");
            });
    }
}
