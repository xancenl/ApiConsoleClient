// Suppress CA2007 for test methods
#pragma warning disable CA2007
using ApiConsoleClient.ApiClient;
using Flurl.Http.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using FluentAssertions;

namespace ApiConsoleClient.Tests;

public class ApiExceptionDetailTests
{
    [Fact]
    public async Task NonJsonErrorBodyStillBuildsException()
    {
        using var http = new HttpTest();
        http.RespondWith("Internal Error", 502, new Dictionary<string,string> { ["Content-Type"] = "text/plain" });

        var client = new ApiClient.ApiClient(new NullLogger<ApiConsoleClient.ApiClient.ApiClient>(), new ApiClientOptions { BaseUrl = new Uri("https://api.test") }, new NoAuth());

        var act = async () => await client.GetHealthAsync(new GetHealthRequest());
        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.StatusCode.Should().Be(502);
        ex.Which.ResponseBody.Should().Contain("Internal Error");
        ex.Which.ProblemDetails.Should().BeNull();
    }

    [Fact]
    public void ToDetailedStringIncludesKeyFields()
    {
        var ex = new ApiException(418, "I'm a teapot", "{ }", null!, "GET", new Uri("https://api.test/health"));
        var s = ex.ToDetailedString();
        s.Should().Contain("418").And.Contain("I'm a teapot").And.Contain("/health");
    }
}
