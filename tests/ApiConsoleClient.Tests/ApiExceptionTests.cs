// Suppress CA2007 in test code – ConfigureAwait(false) is discouraged in test methods
#pragma warning disable CA2007
using System;
using System.Net.Http;
using System.Threading.Tasks;
using ApiConsoleClient.ApiClient;
using Flurl.Http.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using FluentAssertions;

namespace ApiConsoleClient.Tests;

public class ApiExceptionTests
{
    [Fact]
    public async Task ThrowsRichApiExceptionOn5xxWithProblemDetailsParsed()
    {
        using var http = new HttpTest();
    http.RespondWithJson(new { title = "Oops", detail = "fail" }, 500);

        var options = new ApiClientOptions { BaseUrl = new Uri("https://api.test") };
        var client = new ApiClient.ApiClient(new NullLogger<ApiClient.ApiClient>(), options, new NoAuth());

    Func<Task> act = async () => await client.GetHealthAsync(new GetHealthRequest());

        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.StatusCode.Should().Be(500);
        ex.Which.ResponseBody.Should().Contain("Oops");
        ex.Which.Method.Should().Be("GET");
        ex.Which.Url.ToString().Should().EndWith("/health");
        ex.Which.Message.Should().StartWith("HTTP 500");
        ex.Which.ProblemDetails.Should().NotBeNull();
    }
}
