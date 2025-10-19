using System;
using System.Net.Http;
using System.Threading.Tasks;
using ApiConsoleClient.ApiClient;
using Flurl.Http.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using FluentAssertions;

namespace ApiConsoleClient.Tests;

public class PathSerializationTests
{
    [Fact]
    public async Task FormatsDateTimeOffsetAsIsoWithOffset()
    {
        using var http = new HttpTest();
        http.RespondWithJson(new { ok = true });
        var options = new ApiClientOptions { BaseUrl = new Uri("https://api.test") };
        var client = new ApiClient.ApiClient(new NullLogger<ApiClient.ApiClient>(), options, new NoAuth());

        var dto = new GetProductUpdatedsinceUpdatedSinceRequest { UpdatedSince = new DateTimeOffset(2024, 12, 31, 23, 59, 58, TimeSpan.Zero) };
        await client.GetProductUpdatedsinceUpdatedSinceAsync(dto);

        http.ShouldHaveCalled("https://api.test/product/updated_since/2024-12-31T23:59:58+00:00")
            .WithVerb(HttpMethod.Get);
    }

    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public async Task SerializesBooleanPathLowercase(bool flag, string expected)
    {
        using var http = new HttpTest();
        http.RespondWithJson(new { ok = true });
        var options = new ApiClientOptions { BaseUrl = new Uri("https://api.test") };
        var client = new ApiClient.ApiClient(new NullLogger<ApiClient.ApiClient>(), options, new NoAuth());

        var req = new GetProductSkuStockIncludeSplitStockRequest { Sku = "ABC", IncludeSplitStock = flag };
        await client.GetProductSkuStockIncludeSplitStockAsync(req);

        http.ShouldHaveCalled($"https://api.test/product/ABC/stock/{expected}")
            .WithVerb(HttpMethod.Get);
    }
}
