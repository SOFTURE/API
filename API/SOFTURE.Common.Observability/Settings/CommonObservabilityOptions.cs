namespace SOFTURE.Common.Observability.Settings;

internal sealed class CommonObservabilityOptions(bool prometheusEnabled)
{
    public bool PrometheusEnabled { get; } = prometheusEnabled;
}
