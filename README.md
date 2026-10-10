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
| [SOFTURE.Common.Logging](https://www.nuget.org/packages/SOFTURE.Common.Logging) | Structured logging with Serilog, optional Seq and OpenTelemetry (OTLP) export |
| [SOFTURE.Common.Observability](https://www.nuget.org/packages/SOFTURE.Common.Observability) | OpenTelemetry tracing and metrics (OTLP, Prometheus) |
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
dotnet add package SOFTURE.Common.Logging
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

### Logging

Sets up Serilog with console output, an optional Seq sink and an optional OpenTelemetry (OTLP) sink. Minimum level is `Information`, with `Microsoft.*` and `System.*` raised to `Warning`; every event carries the `Application` property and `LogContext` properties.

```csharp
services.AddCommonLogging<AppSettings>(); // console + Seq (when configured) + OTLP (when configured)
services.AddCommonLogging();              // console + OTLP (when configured), no Seq settings needed
```

With `AddCommonLogging<TSettings>()` your settings class implements `ISeqSettings`:
- `Seq:Url` — Seq server endpoint. **Optional**: when it is empty or the section is missing, the Seq sink is skipped and the Seq health check is not registered.
- `Seq:ApiKey` — Seq API key

The OTLP sink is enabled only when an OTLP endpoint is configured (see [OpenTelemetry export](#opentelemetry-export-otlp)). It keeps the whole Serilog pipeline (enrichers, `LogContext`, destructuring) and carries `TraceId`/`SpanId` from `Activity.Current`, so logs written inside a request or a consumer correlate with its trace.

### Observability

Configures OpenTelemetry tracing and metrics with ASP.NET Core, HttpClient, EF Core, Npgsql, runtime and MassTransit instrumentation. Exports traces and metrics over OTLP and metrics via the Prometheus scraping endpoint.

```csharp
services.AddCommonObservability<AppSettings>(); // legacy Observability:Url still honoured
services.AddCommonObservability();              // OpenTelemetry section / OTEL_* variables only

// In the pipeline:
app.UseCommonOpenTelemetry(); // maps the Prometheus scraping endpoint unless it is disabled
```

With `AddCommonObservability<TSettings>()` your settings class implements `IObservabilitySettings`:
- `Observability:Url` — **legacy**, optional OTLP collector endpoint. It is used only when the `OpenTelemetry` section (and the `OTEL_EXPORTER_OTLP_*` variables) name no endpoint. It behaves as before: the URL is used as-is, the protocol defaults to gRPC (or `OTEL_EXPORTER_OTLP_PROTOCOL`), no headers are added. Metrics now go to this URL too (they previously had no endpoint and fell back to `localhost:4317`).

HTTP calls that ship telemetry to the configured OTLP endpoints (for example the Serilog OTLP sink) are excluded from HttpClient tracing, so exports do not generate spans of their own.

### OpenTelemetry export (OTLP)

`SOFTURE.Common.Logging` and `SOFTURE.Common.Observability` read **one** configuration section, `OpenTelemetry`, so logs, traces and metrics go to the same backend with the same resource attributes. The section is read straight from `IConfiguration`, so it does not have to be a property of your settings class (and is not subject to `ValidateSettings`).

```jsonc
"OpenTelemetry": {
  "Endpoint": "https://otlp.example.com/otlp",   // base URL; /v1/logs, /v1/traces, /v1/metrics are appended for http/protobuf
  "Protocol": "HttpProtobuf",                     // HttpProtobuf (default) or Grpc
  "Headers": "Authorization=Basic <token>",       // standard key=value,key2=value2 format; values may be URL-encoded
  "ServiceName": "my-service",                    // default: entry assembly name
  "ServiceNamespace": "my-project",
  "ServiceVersion": "1.2.3",                      // default: entry assembly informational version (without +metadata)
  "DeploymentEnvironment": "production",          // default: ASPNETCORE_ENVIRONMENT / DOTNET_ENVIRONMENT
  "PrometheusEnabled": true                       // Observability only; false skips the Prometheus exporter and endpoint
}
```

| Key | Default | Notes |
|---|---|---|
| `Endpoint` | — | No endpoint (here or in `OTEL_EXPORTER_OTLP_*`) means no OTLP export. For `HttpProtobuf` the per-signal path is appended (`<Endpoint>/v1/logs`, `/v1/traces`, `/v1/metrics`); an endpoint that already ends with it is left alone. For `Grpc` it is used as-is. |
| `Protocol` | `HttpProtobuf` | Also accepts `http/protobuf` and `grpc`. Note that this default differs from the .NET exporter's gRPC default. |
| `Headers` | — | Sent with every export (logs, traces and metrics). |
| `ServiceName` | entry assembly name | Resource attribute `service.name`. |
| `ServiceNamespace` | — | Resource attribute `service.namespace`. |
| `ServiceVersion` | entry assembly version | Resource attribute `service.version`. |
| `DeploymentEnvironment` | hosting environment name | Emitted as `deployment.environment.name` (current semantic conventions) and, for backends that still read it, the deprecated `deployment.environment`. |
| `PrometheusEnabled` | `true` | Read by `SOFTURE.Common.Observability` only. |

Every signal carries the same resource: `service.name`, `service.namespace`, `service.version`, `deployment.environment.name` (plus `deployment.environment`) and `service.instance.id` (the machine/container name). In Grafana these become the `service_name`, `service_namespace` and `deployment_environment_name` labels used to tell projects and environments apart in one stack.

#### Environment variables

The standard OpenTelemetry variables work as an alternative to the section:

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=https://otlp.example.com/otlp
OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf
OTEL_EXPORTER_OTLP_HEADERS=Authorization=Basic%20<token>
OTEL_SERVICE_NAME=my-service
OTEL_RESOURCE_ATTRIBUTES=service.namespace=my-project,deployment.environment.name=production
```

Signal-specific variables (`OTEL_EXPORTER_OTLP_{LOGS|TRACES|METRICS}_ENDPOINT`, `_PROTOCOL`, `_HEADERS`) are honoured too; a signal-specific endpoint is used as-is. The section can also be supplied through the usual .NET configuration providers, e.g. `OpenTelemetry__Endpoint` and `OpenTelemetry__Headers`.

Precedence, per value: the `OpenTelemetry` section (from any configuration provider) → the signal-specific `OTEL_*` variable → the generic `OTEL_*` variable (`OTEL_SERVICE_NAME` before `OTEL_RESOURCE_ATTRIBUTES` for the service name) → the default. In `SOFTURE.Common.Observability` the legacy `Observability:Url` is used only when none of these name an endpoint.

> **Secrets**: the `Headers` value (e.g. a Basic auth token) is a credential. Keep it out of committed `appsettings*.json`: inject it from your secret store or deployment environment as `OpenTelemetry__Headers` or `OTEL_EXPORTER_OTLP_HEADERS`.

#### Migrating from the previous settings

- **Nothing to do to keep the current behaviour.** Existing `Seq` and `Observability:Url` settings work unchanged; nothing is exported over OTLP until an endpoint is configured.
- **To add an OTLP backend**: add the `OpenTelemetry` section (endpoint and headers from secrets). It applies to logs, traces and metrics at once and takes precedence over `Observability:Url`.
- **To drop Seq**: leave `Seq:Url` empty (or remove the section and call `AddCommonLogging()`); the Seq health check goes away with it. If you use `ValidateSettings`, keep the `Seq` section with empty values or remove the `Seq` property from your settings class.
- **To drop `Observability:Url`**: move the endpoint to `OpenTelemetry:Endpoint`. Mind the protocol: the new section defaults to `HttpProtobuf`, the legacy URL used gRPC — set `"Protocol": "Grpc"` when pointing at a gRPC collector port (usually `4317`).
- An application that already sets `OTEL_EXPORTER_OTLP_ENDPOINT` will now also export **logs** there.
- Call `Log.CloseAndFlush()` (or `await Log.CloseAndFlushAsync()`) on shutdown so the last batch of logs reaches Seq and the OTLP backend.

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

Configures MassTransit with RabbitMQ for publishing and consuming messages. Includes in-memory outbox, correlation logging filters, and automatic consumer discovery.

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
