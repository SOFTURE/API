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
| [SOFTURE.Common.Logging](https://www.nuget.org/packages/SOFTURE.Common.Logging) | Structured logging with Serilog and Seq integration |
| [SOFTURE.Common.Observability](https://www.nuget.org/packages/SOFTURE.Common.Observability) | OpenTelemetry tracing and metrics (Prometheus, OTLP) |
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

Sets up Serilog with console output and Seq sink. Automatically registers a Seq health check.

```csharp
services.AddCommonLogging<AppSettings>();
```

Your settings class must implement `ISeqSettings` and provide:
- `Url` — Seq server endpoint
- `ApiKey` — Seq API key

### Observability

Configures OpenTelemetry with ASP.NET Core, HttpClient, EF Core, and Npgsql instrumentation. Exports metrics via Prometheus and traces via OTLP.

```csharp
services.AddCommonObservability<AppSettings>();

// In the pipeline:
app.UseCommonOpenTelemetry();
```

Your settings class must implement `IObservabilitySettings` and provide:
- `Url` — OTLP collector endpoint

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
