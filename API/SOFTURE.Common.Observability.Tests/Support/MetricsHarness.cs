using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using SOFTURE.Common.Observability.Extensions;
using SOFTURE.Common.Observability.Settings;

namespace SOFTURE.Common.Observability.Tests.Support;

internal sealed class MetricsHarness : IDisposable
{
    public const string OtlpEndpoint = "http://localhost:4318";
    public const string SeqUrl = "http://localhost:5341";

    private readonly ServiceProvider _services;
    private readonly MeterProvider _meterProvider;
    private readonly List<MetricSnapshot> _exported = [];

    private MetricsHarness(MetricsSettings metrics)
    {
        var services = new ServiceCollection();

        services
            .AddCommonTelemetry(CreateSettings(metrics))
            .ConfigureOpenTelemetryMeterProvider(builder => builder.AddInMemoryExporter(_exported));

        _services = services.BuildServiceProvider();
        _meterProvider = _services.GetRequiredService<MeterProvider>();
    }

    public static MetricsHarness Start(MetricsSettings metrics)
    {
        return new MetricsHarness(metrics);
    }

    public static TestSettings CreateSettings(MetricsSettings metrics)
    {
        return new TestSettings
        {
            OpenTelemetry = new OpenTelemetrySettings
            {
                Endpoint = OtlpEndpoint,
                Headers = string.Empty,
                ServiceName = "tests",
                ServiceNamespace = "softure",
                Environment = "test",
                PrometheusEnabled = false,
                Metrics = metrics
            },
            Seq = new SeqSettings { Url = SeqUrl, ApiKey = string.Empty }
        };
    }

    public IReadOnlyList<MetricSnapshot> Collect()
    {
        _exported.Clear();
        _meterProvider.ForceFlush();

        return _exported.ToList();
    }

    public void Dispose()
    {
        _services.Dispose();
    }
}
