using Xunit;
using SOFTURE.Common.Observability.Settings;
using SOFTURE.Common.Observability.Tests.Support;

namespace SOFTURE.Common.Observability.Tests.Metrics;

public sealed class RuntimeMetricsTests
{
    private static readonly string[] RuntimeMeterNames = ["System.Runtime", "OpenTelemetry.Instrumentation.Runtime"];

    [Fact]
    public void Runtime_metrics_are_exported_by_default()
    {
        using var harness = MetricsHarness.Start(new MetricsSettings());

        Assert.Contains(harness.Collect(), metric => RuntimeMeterNames.Contains(metric.MeterName));
    }

    [Fact]
    public void Disabled_runtime_instrumentation_exports_no_runtime_metrics()
    {
        using var harness = MetricsHarness.Start(new MetricsSettings { RuntimeEnabled = false });

        Assert.DoesNotContain(harness.Collect(), metric => RuntimeMeterNames.Contains(metric.MeterName));
    }
}
