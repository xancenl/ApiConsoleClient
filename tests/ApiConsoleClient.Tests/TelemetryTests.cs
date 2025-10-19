// Suppress CA2007 for test methods
#pragma warning disable CA2007
using ApiConsoleClient.ApiClient;
using Flurl.Http.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using FluentAssertions;

namespace ApiConsoleClient.Tests;

file sealed class TestSink : IApiTelemetrySink
{
    public List<RequestCompleted> Completed { get; } = new();
    public List<RequestFailed> Failed { get; } = new();
    public void OnRequestCompleted(RequestCompleted evt) => Completed.Add(evt);
    public void OnRequestFailed(RequestFailed evt) => Failed.Add(evt);
}

public class TelemetryTests
{
    [Fact]
    public async Task NonVerboseSuccessEmitsCompletion()
    {
        using var http = new HttpTest();
        http.RespondWithJson(new { ok = true });

        var sink = new TestSink();
        var options = new ApiClientOptions { BaseUrl = new Uri("https://api.test"), TelemetrySink = sink };
        var client = new ApiClient.ApiClient(new NullLogger<ApiClient.ApiClient>(), options, new NoAuth());
        VerboseSwitch.IsVerbose = false;

    await client.GetHealthAsync(new GetHealthRequest());

    sink.Completed.Should().HaveCount(1);
    sink.Completed[0].Status.Should().Be(200);
    sink.Failed.Should().BeEmpty();
    }

    [Fact]
    public async Task NonVerboseFailureEmitsFailure()
    {
        using var http = new HttpTest();
        http.RespondWith("boom", 503);

        var sink = new TestSink();
        var options = new ApiClientOptions { BaseUrl = new Uri("https://api.test"), TelemetrySink = sink };
        var client = new ApiClient.ApiClient(new NullLogger<ApiClient.ApiClient>(), options, new NoAuth());
        VerboseSwitch.IsVerbose = false;

    var act = async () => await client.GetHealthAsync(new GetHealthRequest());
    await act.Should().ThrowAsync<ApiException>();

    sink.Failed.Should().HaveCount(1);
    sink.Failed[0].Status.Should().Be(503);
    // Completed should be empty for non-2xx; failures captured in Failed
    sink.Completed.Should().BeEmpty();
    }
}
