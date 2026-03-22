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
services.AddCommonConsumers<AppSettings>(
    assembly: typeof(Program).Assembly,
    retryCount: 3,
    prefetchCount: 50,
    exponentialRetry: true);
```

Your settings class must implement `IRabbitSettings` and provide:
- `Url` — RabbitMQ connection URL
- `Name` — queue name (consumers only)

Consumer classes are discovered automatically — any non-abstract class implementing `IConsumer<IMessage>` or `IConsumer<IBulkMessage>` in the provided assembly will be registered.

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
