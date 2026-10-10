namespace SOFTURE.Common.Otlp.Settings;

public sealed class OpenTelemetrySettings
{
#if NET8_0_OR_GREATER
    public required string Endpoint { get; init; }
    public required string Headers { get; init; }
    public required string ServiceName { get; init; }
    public required string ServiceNamespace { get; init; }
    public required string Environment { get; init; }
#endif

#if NET6_0
    public string Endpoint { get; init; } = null!;
    public string Headers { get; init; } = null!;
    public string ServiceName { get; init; } = null!;
    public string ServiceNamespace { get; init; } = null!;
    public string Environment { get; init; } = null!;
#endif

    public bool PrometheusEnabled { get; init; } = true;
}
