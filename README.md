# ApiConsoleClient

![CI](https://github.com/xancenl/ApiConsoleClient/actions/workflows/dotnet-desktop.yml/badge.svg)
![Coverage](https://img.shields.io/badge/coverage-codecov-blue?logo=codecov)

A .NET 9 console client that generates a strongly-typed API client and models from an OpenAPI (Swagger) spec and calls endpoints using Flurl.Http.

This project is fully self-contained: it parses the OpenAPI document and generates the client code and DTOs inside the repo. Tests use xUnit and Flurl.Http.Testing.
The HTTP client is built on Flurl with structured logging, a verbose switch, correlation IDs, and a small telemetry abstraction.

## Prerequisites

- .NET SDK 9 (preview)

## Configuration (Environment Variables)

Authentication now uses HTTP Basic Auth.

Set the following (you can use either the primary or fallback names):

| Variable | Required | Notes |
| -------- | -------- | ----- |
| `API_BASE_URL` | Yes | Base URL of the API (e.g. `https://api-v6.monta.nl`) |
| `USERNAME` or `API_USERNAME` | Yes (for authenticated calls) | HTTP Basic username |
| `PASSWORD` or `API_PASSWORD` | Yes (for authenticated calls) | HTTP Basic password |
| `API_ENABLE_CONSOLE_TELEMETRY` | No | `true` to print per-request telemetry to console |
| `API_INCLUDE_REQID_QUERY` | No | `true` to append `_reqId` to URLs in verbose mode |

If `USERNAME`/`PASSWORD` are not set, the client will emit a one-time warning and send no Authorization header (calls may 401).

### .env Pattern (Optional)

You can maintain a local `.env` file (not committed) and load it in PowerShell:

```powershell
Get-Content .env | ForEach-Object {
  if ($_ -match '^(?<k>[^#=]+)=(?<v>.+)$') { Set-Item -Path Env:$($matches.k.Trim()) -Value ($matches.v.Trim()) }
}
```

Example `.env` file:

```dotenv
API_BASE_URL=https://api-v6.monta.nl
USERNAME=your-user
PASSWORD=your-pass
```

## Build

```powershell
 dotnet build ApiConsoleClient.sln -c Release
```

## Run

```powershell
 dotnet run --project src/ApiConsoleClient -- \
   --operation <operationId> \
   --input '{"param":"value"}' \
   --dump
```

Example (health check):

```powershell
$env:API_BASE_URL='https://api-v6.monta.nl'; $env:USERNAME='demo'; $env:PASSWORD='secret'; \
  dotnet run --project src/ApiConsoleClient -- --operation GetHealth
```

If `--operation` is omitted, the app lists all available operationIds.

## Tests and Coverage

```powershell
 dotnet test ApiConsoleClient.sln -c Release --collect:"XPlat Code Coverage" --settings coverlet.runsettings
```

Notes:

- We use the coverlet data collector with a runsettings file that excludes generated code and DTOs from coverage (focuses the gate on testable behavior).
- To produce a local text summary, install ReportGenerator and aggregate Cobertura outputs:

  ```powershell
  dotnet tool install -g dotnet-reportgenerator-globaltool
  reportgenerator -reports:"tests/ApiConsoleClient.Tests/TestResults/**/coverage.cobertura.xml" -targetdir:"coverage" -reporttypes:"TextSummary;Cobertura"
  Get-Content coverage/Summary.txt
  ```

In CI, coverage is collected via Coverlet (collector) on Windows/Linux/macOS and merged. A summary is uploaded as an artifact, and the job enforces a coverage gate.

### Codecov

CI uploads coverage to Codecov using tokenless OpenID Connect (no `CODECOV_TOKEN` needed). After the first successful upload, replace the generic coverage badge at the top with your project-specific Codecov badge for real‑time metrics.

The gate currently fails the workflow if merged line coverage is below 48%.

## Adding/Re-generating Endpoints

The generator under `tools/ApiConsoleClient.Generator` downloads/parses the OpenAPI and writes:

- Models to `src/ApiConsoleClient/Models`
- Client interface/implementation to `src/ApiConsoleClient/ApiClient`

Rerun the generator anytime the spec changes (see How to regenerate section inside the tool usage printed by the generator).

## CLI options

| Option | Required | Description |
|---|---|---|
| `--operation` | Yes | The operationId to execute |
| `--input` | No | JSON string with parameters/body |
| `--dump` | No | Prints raw response JSON to console |
| `--help` | No | Prints help |
| `--verbose`, `-v` | No | Logs request URLs to stdout |

### Verbose mode and structured logging

When you add `--verbose` (or `-v`), the client emits structured request/response logs with a correlation id and timing:

- BEGIN: request id, method, URL
- END: request id, status code, method, URL, elapsed ms, content length
- ERR: request id, status (if available), method, URL, elapsed ms, exception

Notes:

- A header `X-Request-Id` is attached to each request in verbose mode.
- By default, the correlation id is not appended to the URL. To include it as a query parameter (`_reqId=<id>`), set `IncludeRequestIdAsQuery` to `true` in `ApiClientOptions` at composition time.

Example run with verbose:

```powershell
$env:API_BASE_URL='https://api-v6.monta.nl'; $env:USERNAME='demo'; $env:PASSWORD='secret'
dotnet run --project src/ApiConsoleClient -- --operation GetHealth --verbose
```

#### Telemetry sink (optional)

For custom metrics, you can provide an `IApiTelemetrySink` via `ApiClientOptions.TelemetrySink` to receive per-request completion/failure events with latency and sizes. This is useful for exporting to OpenTelemetry, Prometheus, or Application Insights.

Built-in examples:

- `ConsoleTelemetrySink` (enabled by setting `API_ENABLE_CONSOLE_TELEMETRY=true`)
- `OpenTelemetryAdapterSink` (emits Activities; requires your host to configure OpenTelemetry to collect them)

### Example environment file

An `example.env` is included to illustrate required variables. Copy it to `.env` and fill in real credentials (do not commit your `.env`).

## Notes

- Serialization uses System.Text.Json with camelCase and ignores null by default.
- Non-success responses are mapped to ApiException with detailed context (status, reason, body, problem object, method, URL).
- The generator currently supports OpenAPI 3.0 and 3.1.
- Not all OpenAPI constructs are implemented (e.g. complex polymorphism, discriminators).
- If you regenerate code, any local manual changes to generated `.g.cs` files will be overwritten.
