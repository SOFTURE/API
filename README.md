# SOFTURE

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![NuGet](https://img.shields.io/nuget/v/SOFTURE.Common.CQRS.svg?label=NuGet)](https://www.nuget.org/profiles/SOFTURE)
[![.NET](https://img.shields.io/badge/.NET-6.0%20%7C%208.0%20%7C%209.0%20%7C%2010.0-purple)](https://dotnet.microsoft.com/)

A collection of reusable .NET libraries providing common infrastructure for building distributed, observable, and resilient microservices. Each module is published as an independent NuGet package and can be adopted incrementally.

## Packages

| Package | Description |
|---------|-------------|
| [SOFTURE.Common.Authentication](https://www.nuget.org/packages/SOFTURE.Common.Authentication) | JWT Bearer authentication setup and configuration |
| [SOFTURE.Common.CQRS](https://www.nuget.org/packages/SOFTURE.Common.CQRS) | CQRS middleware and validation behaviors for MediatR pipelines |
| [SOFTURE.Common.Correlation](https://www.nuget.org/packages/SOFTURE.Common.Correlation) | Request correlation ID tracking for distributed tracing |
| [SOFTURE.Common.HealthCheck](https://www.nuget.org/packages/SOFTURE.Common.HealthCheck) | Health check framework with standardized response format |
| [SOFTURE.Common.Observability](https://www.nuget.org/packages/SOFTURE.Common.Observability) | Logs, traces and metrics: Serilog and OpenTelemetry with OTLP export, Prometheus and optional Seq |
| [SOFTURE.Common.Resilience](https://www.nuget.org/packages/SOFTURE.Common.Resilience) | HTTP resilience policies — retry, circuit breaker, hedging, fallback |
| [SOFTURE.Common.StronglyTypedIdentifiers](https://www.nuget.org/packages/SOFTURE.Common.StronglyTypedIdentifiers) | Strongly-typed ID abstractions with EF Core and FastEndpoints support |
| [SOFTURE.Common.Web](https://www.nuget.org/packages/SOFTURE.Common.Web) | Web application bootstrap helpers — culture enforcement and data protection keys persistence |
| [SOFTURE.MessageBroker.Rabbit](https://www.nuget.org/packages/SOFTURE.MessageBroker.Rabbit) | RabbitMQ message publishing and consuming via MassTransit |

## Getting Started

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) 6.0, 8.0, 9.0, or 10.0

### Installation

Install the packages you need via NuGet:

```bash
dotnet add package SOFTURE.Common.Authentication
dotnet add package SOFTURE.Common.CQRS
dotnet add package SOFTURE.Common.Correlation
dotnet add package SOFTURE.Common.HealthCheck
dotnet add package SOFTURE.Common.Observability
dotnet add package SOFTURE.Common.Resilience
dotnet add package SOFTURE.Common.StronglyTypedIdentifiers
dotnet add package SOFTURE.Common.Web
dotnet add package SOFTURE.MessageBroker.Rabbit
```

## Usage

All modules integrate through `IServiceCollection` extension methods in your `Program.cs` or `Startup.cs`.

### Authentication

Configures JWT Bearer authentication with symmetric key signing.

```csharp
services.AddCommonAuthentication<AppSettings>();
```

Your settings class must implement `IAuthenticationSettings` and provide:
- `JwtSecret` — symmetric signing key
- `ValidAudience` — expected token audience
- `ValidIssuer` — expected token issuer

### CQRS

Registers MediatR pipeline behaviors for automatic command validation using FluentValidation.

```csharp
services.AddMiddlewares();
```

### Observability (logs, traces, metrics)

One package and one call set up logging (Serilog), tracing and metrics (OpenTelemetry). All three signals are exported over OTLP **http/protobuf** to the same endpoint, with the same headers and the same resource attributes. Seq is optional.

```csharp
services.AddCommonObservability<AppSettings>();

// In the pipeline (Prometheus scraping endpoint, unless disabled):
app.UseCommonObservability();
```

What it registers:
- **Logs**: Serilog with the console sink and the OTLP sink. Minimum level is `Information`, with `Microsoft.*` and `System.*` raised to `Warning`. Every event carries the `Application` property and `LogContext` properties. `TraceId`/`SpanId` come from `Activity.Current`, so logs correlate with traces. When your settings class also implements `ISeqSettings`, the Seq sink and the Seq health check are added.
- **Traces**: ASP.NET Core, HttpClient, EF Core, Npgsql and MassTransit instrumentation. HTTP calls to the OTLP endpoint itself are not traced.
- **Metrics**: runtime, HttpClient and ASP.NET Core instrumentation, exported over OTLP and through the Prometheus exporter unless `PrometheusEnabled` is `false`. The default `Minimal` profile keeps the active series count low (see [Metrics profile](#metrics-profile)). Requests the package sends itself (OTLP export, the Seq sink) are left out of the HttpClient metrics on .NET 8 and later.

#### Settings

Your settings class implements `IOpenTelemetrySettings`, and optionally `ISeqSettings`:

```csharp
public sealed class AppSettings : IOpenTelemetrySettings, ISeqSettings
{
    public required OpenTelemetrySettings OpenTelemetry { get; init; }
    public required SeqSettings Seq { get; init; }
}
```

```jsonc
"OpenTelemetry": {
  "Endpoint": "https://otlp.example.com/otlp",
  "Headers": "Authorization=Basic <token>",
  "ServiceName": "my-service",
  "ServiceNamespace": "my-project",
  "Environment": "production",
  "PrometheusEnabled": true,
  "Metrics": {
    "Profile": "Minimal",
    "DroppedInstruments": []
  }
},
"Seq": {
  "Url": "http://seq.example.com",
  "ApiKey": "<api-key>"
}
```

| Key | Required | Meaning |
|---|---|---|
| `OpenTelemetry:Endpoint` | yes | Base OTLP/HTTP URL. `/v1/logs`, `/v1/traces` and `/v1/metrics` are appended per signal; an endpoint that already ends with the signal path is left alone. |
| `OpenTelemetry:Headers` | yes | Sent with every export, in the `key=value,key2=value2` format. Values may be URL-encoded (`Basic%20<token>`). Use an empty string for a collector without authentication. |
| `OpenTelemetry:ServiceName` | yes | Resource attribute `service.name`. |
| `OpenTelemetry:ServiceNamespace` | yes | Resource attribute `service.namespace`. |
| `OpenTelemetry:Environment` | yes | Resource attribute `deployment.environment.name`, plus the deprecated `deployment.environment` with the same value for backends that still read it. |
| `OpenTelemetry:PrometheusEnabled` | no (default `true`) | `false` skips the Prometheus exporter and its scraping endpoint. |
| `OpenTelemetry:Metrics:Profile` | no (default `Minimal`) | `Minimal` or `Full`, see [Metrics profile](#metrics-profile). |
| `OpenTelemetry:Metrics:DroppedInstruments` | no (default empty) | Extra instrument names to drop, in both profiles. A trailing `*` matches a prefix (`kestrel.*`). |
| `Seq:Url`, `Seq:ApiKey` | with `ISeqSettings` | Seq server and API key. |

Every signal also carries `service.version` (the entry assembly's informational version, without `+metadata`) and `service.instance.id` (the machine or container name). In Grafana these become the `service_name`, `service_namespace` and `deployment_environment_name` labels used to tell projects and environments apart in one stack.

Validate the settings at startup with `SOFTURE.Settings`. `ValidateSettings` fails when any section or value is missing, so the package works only with real values:

```csharp
builder.Configuration.ValidateSettings<AppSettings>();
builder.Services.ConfigureSettings<AppSettings>(builder.Configuration);
```

Values can come from any configuration provider, for example the environment variables `OpenTelemetry__Endpoint` and `OpenTelemetry__Headers`.

> **Secrets**: `OpenTelemetry:Headers` holds a credential. Never commit it to `appsettings*.json`; inject it from your secret store or the deployment environment (`OpenTelemetry__Headers`).

Call `Log.CloseAndFlush()` on shutdown so the last batch of logs reaches the backend.

#### Metrics profile

Every histogram bucket is a separate series, so the default instrumentation quickly fills a backend that bills or limits by active series. The `Minimal` profile (the default):

- drops `dns.lookup.duration`, `http.client.connection.duration`, `http.client.request.time_in_queue`, `http.client.open_connections`, `http.client.active_requests`, `aspnetcore.authentication.authenticate.duration`, `aspnetcore.routing.match_attempts`, `kestrel.connection.duration`, `aspnetcore.rate_limiting.request_lease.duration` and `aspnetcore.memory_pool.*`;
- exports `http.server.request.duration` and `http.client.request.duration` with the buckets `0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10` seconds (10 buckets instead of 15);
- keeps everything else, including the `dotnet.*` runtime metrics and `aspnetcore.rate_limiting.requests`.

`Full` exports every instrument with the default buckets. `DroppedInstruments` applies on top of either profile, so a service can switch to `Full` and still drop what it does not need.

#### Result logging helpers

`LogError` and `Log` for `Result` / `Result<T>` keep their signatures and now live in `SOFTURE.Common.Observability.Logging`.

#### Migrating from SOFTURE.Common.Logging and the previous Observability

**This is a breaking change and needs a new major version of `SOFTURE.Common.Observability`.** `SOFTURE.Common.Logging` is discontinued: its code now lives in `SOFTURE.Common.Observability`. Its last published version stays on NuGet, but it gets no further releases.

1. Replace the `SOFTURE.Common.Logging` package reference with `SOFTURE.Common.Observability`.
2. Implement `IOpenTelemetrySettings` and add the `OpenTelemetry` section, with headers from secrets.
3. `IObservabilitySettings`, `ObservabilitySettings` and `Observability:Url` are removed. The endpoint is `OpenTelemetry:Endpoint`. The old URL used gRPC; export is now http/protobuf only, so point it at an OTLP/HTTP receiver (usually port `4318`).
4. Replace `AddCommonLogging<TSettings>()` and `AddCommonObservability<TSettings>()` with a single `AddCommonObservability<TSettings>()` call, and `UseCommonOpenTelemetry()` with `UseCommonObservability()`.
5. To keep Seq, keep `ISeqSettings` on the settings class. Its namespace is now `SOFTURE.Common.Observability.Settings`.
6. Change `using SOFTURE.Common.Logging.Extensions;` to `using SOFTURE.Common.Observability.Logging;` for the `Result` logging helpers.
7. Run `ValidateSettings<AppSettings>()` at startup so a missing value fails fast.

### Resilience

Registers a named resilience pipeline (`"retry"`) with hedging, fallback, retry (exponential backoff with jitter), and circuit breaker strategies.

```csharp
services.AddCommonResilience();
```

Usage in application code:

```csharp
// Inject ResiliencePipelineProvider<string>
var pipeline = pipelineProvider.GetPipeline<HttpResponseMessage>("retry");
var response = await pipeline.ExecuteAsync(
    async token => await httpClient.GetAsync("https://api.example.com", token), ct);
```

### Correlation

Registers a scoped correlation ID provider for request tracking across services.

```csharp
services.AddCommonCorrelationProvider();
```

### Health Checks

Registers custom health checks with a standardized `/hc` endpoint.

```csharp
services.AddCommonHealthCheck<MyCustomHealthCheck>();

// In the pipeline:
app.MapCommonHealthChecks();
```

Health check classes must extend `CheckBase` and implement `ICommonHealthCheck`.

### Message Broker (RabbitMQ)

Configures MassTransit with RabbitMQ for publishing and consuming messages. Includes in-memory outbox, correlation logging filters, and automatic consumer discovery. MassTransit usage telemetry (reports to `usage-tracking.masstransit.io`) is disabled.

**Publisher:**

```csharp
services.AddCommonPublisher<AppSettings>();
```

**Consumer:**

```csharp
services.AddCommonConsumers<AppSettings>(typeof(Program).Assembly, options =>
{
    options.Default.PrefetchCount = 32;
    options.Default.ConcurrentMessageLimit = 16;
});
```

Your settings class must implement `IRabbitSettings` and provide:
- `Url` — RabbitMQ connection URL
- `Name` — queue name (consumers only)
- `Consumers` — optional endpoint tuning, see below

#### Configuring endpoints from `appsettings.json`

Prefetch, concurrency, retry and the kill switch are tuning values that belong in configuration rather than in code. `RabbitSettings.Consumers` binds them, and `ApplyFrom` transfers them onto the endpoint options:

```jsonc
"Rabbit": {
  "Name": "App.Worker",
  "Url": "amqp://guest:guest@localhost:5672",
  "Consumers": {
    "PrefetchCount": 64,
    "ConcurrentMessageLimit": 24,
    "Retry": { "Count": 5, "IsExponential": true, "MinIntervalSeconds": 1, "MaxIntervalSeconds": 120 },
    "Groups": {
      "AI": {
        "PrefetchCount": 16,
        "ConcurrentMessageLimit": 4,
        "KillSwitch": { "Enabled": true, "ActivationThreshold": 5, "TrackingPeriodSeconds": 600, "RestartSeconds": 300 }
      }
    }
  }
}
```

```csharp
var rabbit = configuration.Get<AppSettings>()!.Rabbit;

services.AddCommonConsumers<AppSettings>(assembly, options =>
{
    options.Default.ApplyFrom(rabbit.Consumers);

    options.AddGroup("AI", ConsumerSelectors.ForNamespace("App.Consumers.Ai"),
        group => group.ApplyFrom(rabbit.Consumers.Group("AI")));
});
```

Every field has a default, and `Consumers` itself defaults to an empty instance — applications that do not configure the section keep working unchanged. The kill switch stays off unless `Enabled` is set, so adopting the section never silently changes behaviour.

Consumer classes are discovered automatically — any non-abstract class implementing `IConsumer<IMessage>` or `IConsumer<IBulkMessage>` in the provided assembly will be registered.

#### Endpoint options

| Option | Default | Purpose |
|---|---|---|
| `PrefetchCount` | `32` | How many messages the broker pushes to the endpoint. |
| `ConcurrentMessageLimit` | `16` | How many messages are consumed **in parallel**. Leave it `null` and concurrency equals `PrefetchCount` — each in-flight message holds a consumer slot and, typically, a database connection, so an unbounded value will exhaust a connection pool under a burst. |
| `Retry` | 5 attempts, exponential | Retry policy, including exceptions that skip retry entirely. |
| `ConfigureRetry` | — | Raw `Action<IRetryConfigurator>` when the declarative settings are not enough. Takes precedence over `Retry`. |
| `KillSwitch` | off | Stops the endpoint when the failure ratio exceeds a threshold, restarts it after a timeout. |
| `RateLimit` | off | Caps throughput to N messages per interval. |
| `ConsumeTimeout` | off | Aborts a consumer that runs longer than the timeout. |

`ConcurrentMessageLimit` must not exceed `PrefetchCount` — the configuration is validated at startup and throws otherwise.

#### Retry and permanent failures

Retrying a failure that cannot heal only burns throughput: an in-process retry holds its consumer slot for the whole backoff. Declare such exceptions so they bypass retry and go straight to the error queue:

```csharp
options.Default.WithRetry(r =>
{
    r.Count = 5;
    r.MinInterval = TimeSpan.FromSeconds(1);
    r.MaxInterval = TimeSpan.FromMinutes(2);
    r.Ignore<ValidationException>();
    r.Ignore<UnparsableResultException>();
});
```

#### Kill switch

When a downstream dependency is down — an exhausted API budget, an overloaded database — retrying every message wastes calls and dead-letters work that would have succeeded minutes later. The kill switch stops the endpoint instead, leaving messages **in the main queue** rather than the error queue, and restarts it after `RestartTimeout`:

```csharp
options.Default.WithKillSwitch(k =>
{
    k.ActivationThreshold = 10;
    k.TripThreshold = 0.5;
    k.TrackingPeriod = TimeSpan.FromMinutes(1);
    k.RestartTimeout = TimeSpan.FromMinutes(5);
});
```

`TripThreshold` is a fraction in the `(0, 1]` range — `0.5` trips the endpoint when half of the tracked messages fail.

#### Consumer groups

By default every consumer shares one receive endpoint, one prefetch value and one retry policy — so a burst on one message type starves every other consumer, and a kill switch stops all of them at once. Groups split consumers across dedicated endpoints named `{Name}{GroupSeparator}{group}` — with the default separator, a `bulk` group on queue `App.Worker` becomes `App.Worker.bulk`. Set `options.GroupSeparator` to change it.

```csharp
services.AddCommonConsumers<AppSettings>(typeof(Program).Assembly, options =>
{
    options.Default.PrefetchCount = 16;
    options.Default.ConcurrentMessageLimit = 8;

    options.AddBulkGroup("bulk", g =>
    {
        g.PrefetchCount = 64;
        g.ConcurrentMessageLimit = 24;
    });

    options.AddGroup("ai", ConsumerSelectors.ForNamespace("App.Consumers.Ai"), g =>
    {
        g.ConcurrentMessageLimit = 4;
        g.WithRateLimit(limit: 60, interval: TimeSpan.FromMinutes(1));
        g.WithKillSwitch();
    });
});
```

Consumers matching no group stay on the default endpoint. Selectors are composable via `ConsumerSelectors.ForMessages<TMarker>()`, `ForMessage<TMessage>()`, `ForConsumers(...)`, `ForNamespace(...)` and `Any(...)`.

> Adding a group changes the queue topology — the new queue starts empty while the old one may still hold messages. Drain the existing queue before deploying a grouping change.

#### Breaking change: the `retryCount` / `prefetchCount` overload is gone

**This is a breaking change — call sites using the positional overload will not compile.** That is deliberate: the old overload could not express a concurrency bound, and silently discarded half of what it appeared to configure.

It assigned `PrefetchCount` and the retry policy per consumer inside a loop even though both are endpoint-level settings, so whichever loop ran last won. Applications with at least one `IBulkMessage` consumer therefore ran *every* consumer at the bulk prefetch with the exponential policy, discarding the `PrefetchCount = 1` and `Immediate` retry intended for `IMessage` consumers. Migrating is a chance to state what you actually want rather than inherit that accident.

```csharp
// before
services.AddCommonConsumers<AppSettings>(
    assembly, retryCount: 10, prefetchCount: 1000, exponentialRetry: true);

// after
services.AddCommonConsumers<AppSettings>(assembly, options =>
{
    options.Default.PrefetchCount = 64;
    options.Default.ConcurrentMessageLimit = 24;
    options.Default.WithRetry(r => { r.Count = 5; r.IsExponential = true; });
});
```

Pick `ConcurrentMessageLimit` from what a single consumer holds while it runs — typically a database connection. Keeping it comfortably below the connection pool size is the point of the setting.

### Strongly Typed Identifiers

Provides type-safe entity identifiers with EF Core value converters and JSON serialization support for FastEndpoints.

**EF Core configuration (in `DbContext`):**

```csharp
protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
{
    configurationBuilder.ConfigureStronglyIdentifiers<LanguageAssemblyMarker>();
}

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ConfigureStronglyIdentifiers();
}
```

**JSON serialization:**

```csharp
jsonOptions.RegisterStronglyTypedIdConverters<LanguageAssemblyMarker>();
```

Supports `Guid`, `int`, and `long` identifier value types.

### Web

Bootstrap helpers for ASP.NET Core applications — globally enforces a culture and persists Data Protection keys to disk.

**Culture enforcement:**

```csharp
services.AddCommonCulture(new CultureInfo("pl-PL"));

// In the pipeline (web APIs only — applies the enforced culture to every request):
app.UseRequestLocalization();
```

Sets `CultureInfo.DefaultThreadCurrentCulture` / `DefaultThreadCurrentUICulture` and restricts `RequestLocalizationOptions` to the provided culture as the single supported one — requests with `Accept-Language` mismatches fall back to the enforced culture.

**Data Protection keys persistence:**

```csharp
services.AddCommonDataProtection("/tmp/dataprotection-keys");
```

Persists the ASP.NET Core Data Protection key ring to the given filesystem path — required for containerized deployments where keys must survive restarts and be shared across instances.

**Request context logging middleware:**

```csharp
// In the pipeline:
app.UseCommonRequestContextLogging();
```

Reads the `X-Correlation-ID` header (or generates one), stores it in `ICorrelationProvider`, and enriches Serilog `LogContext` with a `CorrelationId` property for the duration of the request. Requires `SOFTURE.Common.Correlation` (transitive) and Serilog for log enrichment.

## Supported Frameworks

| Framework | Status |
|-----------|--------|
| .NET 6.0  | Supported |
| .NET 8.0  | Supported |
| .NET 9.0  | Supported |
| .NET 10.0 | Supported |

## Contributing

Contributions are welcome! To get started:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Commit your changes (`git commit -m 'Add my feature'`)
4. Push to the branch (`git push origin feature/my-feature`)
5. Open a Pull Request

### Building locally

```bash
cd API
dotnet restore
dotnet build
```

### Releasing

Each package has its own GitHub Actions workflow. To release a new version:

1. Create a GitHub Release with a version tag (e.g., `1.0.0`)
2. The corresponding workflow will pack and push the package to NuGet.org

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.
