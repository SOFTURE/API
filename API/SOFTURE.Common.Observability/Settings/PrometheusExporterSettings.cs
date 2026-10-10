namespace SOFTURE.Common.Observability.Settings;

internal sealed class PrometheusExporterSettings(bool isEnabled)
{
    public bool IsEnabled { get; } = isEnabled;
}
