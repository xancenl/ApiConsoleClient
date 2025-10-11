using ApiConsoleClient;
using ApiConsoleClient.ApiClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole();
builder.Configuration
	.AddEnvironmentVariables();

builder.Services.AddSingleton<ApiClientOptions>(sp =>
{
	var cfg = sp.GetRequiredService<IConfiguration>();
	var baseUrl = cfg["API_BASE_URL"] ?? throw new InvalidOperationException("API_BASE_URL env var is required");
	var enableConsoleTelemetry = (cfg["API_ENABLE_CONSOLE_TELEMETRY"] ?? "false").Equals("true", StringComparison.OrdinalIgnoreCase);
	var includeReqIdQuery = (cfg["API_INCLUDE_REQID_QUERY"] ?? "false").Equals("true", StringComparison.OrdinalIgnoreCase);
	return new ApiClientOptions
	{
		BaseUrl = new Uri(baseUrl),
		Timeout = TimeSpan.FromSeconds(100),
		UserAgent = "ApiConsoleClient/1.0",
		DefaultHeaders = new(),
		IncludeRequestIdAsQuery = includeReqIdQuery,
		TelemetrySink = enableConsoleTelemetry ? new ApiConsoleClient.Telemetry.ConsoleTelemetrySink() : null
	};
});

builder.Services.AddSingleton<IAuthHeaderProvider, BasicAuthHeaderProvider>();
builder.Services.AddSingleton<IApiClient, ApiClient>();
builder.Services.AddSingleton<App>();

var host = builder.Build();

var app = host.Services.GetRequiredService<App>();
return await app.RunAsync(args).ConfigureAwait(false);
