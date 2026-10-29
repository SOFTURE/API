using OpenTelemetry.Metrics;
using SOFTURE.Common.Observability.Abstractions;
using SOFTURE.Common.Observability.Models;
using SOFTURE.Common.Observability.Relays;
using SOFTURE.Common.Observability.Settings;

namespace SOFTURE.Common.Observability.Extensions;

internal static class RequestDurationRelayMeterProviderBuilderExtensions
{
    public static MeterProviderBuilder AddRequestDurationRelay(
        this MeterProviderBuilder builder,
        RequestDurationInstrument instrument,
        IRequestFilter filter,
        MetricsSettings settings)
    {
        return builder
            .AddMeter(RequestDurationInstrument.RelayMeterName)
            .AddInstrumentation(() => new RequestDurationRelay(instrument, filter, settings));
    }
}
