#if NET8_0_OR_GREATER
using System.Diagnostics.Metrics;

namespace SOFTURE.Common.Observability.Telemetry;

internal sealed class UnobservedMeterFactory : IMeterFactory
{
    public const string MeterName = "SOFTURE.Common.Observability.Export";

    public static readonly UnobservedMeterFactory Instance = new();

    private readonly Meter _meter = new(MeterName);

    public Meter Create(MeterOptions options)
    {
        return _meter;
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}
#endif
