using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SOFTURE.Common.Observability.Settings;
using SOFTURE.Common.Otlp;
using SOFTURE.Settings.Extensions;

namespace SOFTURE.Common.Observability;

public static class DependencyInjection
{
    /// <summary>
    /// Configures OpenTelemetry tracing and metrics. Exports over OTLP to the endpoint of the <c>OpenTelemetry</c>
    /// section (or the standard <c>OTEL_EXPORTER_OTLP_*</c> variables), falling back to the legacy
    /// <c>Observability:Url</c>, and exposes the Prometheus exporter unless <c>OpenTelemetry:PrometheusEnabled</c> is false.
    /// </summary>
    public static IServiceCollection AddCommonObservability<TSettings>(this IServiceCollection services)
        where TSettings : IObservabilitySettings
    {
        var settings = services.GetSettings<TSettings, ObservabilitySettings?>(x => x.Observability);

        return services.AddCommonObservabilityCore(settings?.Url);
    }

    /// <summary>
    /// Configures OpenTelemetry tracing and metrics from the <c>OpenTelemetry</c> section and the standard
    /// <c>OTEL_EXPORTER_OTLP_*</c> variables only.
    /// </summary>
    public static IServiceCollection AddCommonObservability(this IServiceCollection services)
    {
        return services.AddCommonObservabilityCore(legacyUrl: null);
    }

    public static WebApplication UseCommonOpenTelemetry(this WebApplication app)
    {
        var options = app.Services.GetService<CommonObservabilityOptions>();

        if (options?.PrometheusEnabled ?? true)
        {
            app.UseOpenTelemetryPrometheusScrapingEndpoint();
        }

        return app;
    }

    private static IServiceCollection AddCommonObservabilityCore(this IServiceCollection services, string? legacyUrl)
    {
        var otlp = OtlpConfiguration.FromServices(services);
        var plan = ObservabilityExportPlan.Create(otlp, legacyUrl);
        var resourceAttributes = otlp.ResolveResourceAttributes();

        services.AddSingleton(new CommonObservabilityOptions(otlp.PrometheusEnabled));

        services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddAttributes(resourceAttributes))
            .WithMetrics(metrics =>
            {
                // INFO: here we can add our custom metrics by using AddMeter() method
                metrics
                    .AddRuntimeInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation();

                if (otlp.PrometheusEnabled)
                {
                    metrics.AddPrometheusExporter();
                }

                if (plan.Metrics is not null)
                {
                    metrics.AddOtlpExporter(otlpOptions => plan.Metrics.ApplyTo(otlpOptions));
                }
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(DiagnosticConfig.MassTransitSource)
                    .AddAspNetCoreInstrumentation(_ =>
                    {
                        // INFO: Extra configuration for ASP.NET Core Instrumentation
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        // Requests that ship telemetry (e.g. the Serilog OTLP sink) would otherwise produce
                        // a span per export batch.
                        options.FilterHttpRequestMessage = request => !plan.IsExportRequest(request.RequestUri);
                    })
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddNpgsql();

                if (plan.Traces is not null)
                {
                    tracing.AddOtlpExporter(otlpOptions => plan.Traces.ApplyTo(otlpOptions));
                }
            });

        return services;
    }
}

internal sealed class CommonObservabilityOptions(bool prometheusEnabled)
{
    public bool PrometheusEnabled { get; } = prometheusEnabled;
}

/// <summary>
/// Where traces and metrics go. <see cref="OtlpConfiguration"/> wins when it resolves an endpoint;
/// otherwise the legacy <c>Observability:Url</c> is used exactly as before (endpoint only, protocol and
/// headers left to the exporter defaults and <c>OTEL_EXPORTER_OTLP_*</c> variables).
/// </summary>
internal sealed class ObservabilityExportPlan
{
    private readonly IReadOnlyList<Uri> _exportEndpoints;

    private ObservabilityExportPlan(SignalExport? traces, SignalExport? metrics, IReadOnlyList<Uri> exportEndpoints)
    {
        Traces = traces;
        Metrics = metrics;
        _exportEndpoints = exportEndpoints;
    }

    public SignalExport? Traces { get; }
    public SignalExport? Metrics { get; }

    public static ObservabilityExportPlan Create(OtlpConfiguration otlp, string? legacyUrl)
    {
        var legacyEndpoint = string.IsNullOrWhiteSpace(legacyUrl) ? null : new Uri(legacyUrl);

        var traces = SignalExport.From(otlp.ResolveExport(OtlpSignal.Traces), legacyEndpoint);
        var metrics = SignalExport.From(otlp.ResolveExport(OtlpSignal.Metrics), legacyEndpoint);
        var logs = otlp.ResolveExport(OtlpSignal.Logs);

        var exportEndpoints = new[] { traces?.Endpoint, metrics?.Endpoint, logs?.Endpoint }
            .Where(x => x is not null)
            .Select(x => x!)
            .ToList();

        return new ObservabilityExportPlan(traces, metrics, exportEndpoints);
    }

    public bool IsExportRequest(Uri? requestUri) =>
        _exportEndpoints.Any(endpoint => OtlpConfiguration.IsSameOrigin(endpoint, requestUri));
}

internal sealed class SignalExport
{
    private readonly OtlpExportTarget? _target;

    private SignalExport(OtlpExportTarget? target, Uri endpoint)
    {
        _target = target;
        Endpoint = endpoint;
    }

    public Uri Endpoint { get; }

    public static SignalExport? From(OtlpExportTarget? target, Uri? legacyEndpoint)
    {
        if (target is not null)
            return new SignalExport(target, target.Endpoint);

        return legacyEndpoint is null ? null : new SignalExport(null, legacyEndpoint);
    }

    public void ApplyTo(OtlpExporterOptions options)
    {
        // Setting Endpoint in code disables the exporter's own /v1/{signal} suffixing for http/protobuf,
        // so OtlpConfiguration already returns the full per-signal URL.
        options.Endpoint = Endpoint;

        if (_target is null)
            return;

        options.Protocol = _target.Protocol == OtlpTransport.Grpc
            ? OtlpExportProtocol.Grpc
            : OtlpExportProtocol.HttpProtobuf;

        if (_target.Headers is not null)
            options.Headers = _target.Headers;
    }
}
