using System.Diagnostics.Metrics;

namespace SOFTURE.Common.Observability.Models;

internal sealed class RequestDurationInstrument
{
    public const string RelayMeterName = "SOFTURE.Common.Observability.Http";

#if NET8_0_OR_GREATER
    private const string ServerSourceMeterName = "Microsoft.AspNetCore.Hosting";
    private const string ClientSourceMeterName = "System.Net.Http";
#else
    private const string ServerSourceMeterName = "OpenTelemetry.Instrumentation.AspNetCore";
    private const string ClientSourceMeterName = "OpenTelemetry.Instrumentation.Http";
#endif

    public static readonly RequestDurationInstrument HttpServerRequests =
        new("http.server.request.duration", "Duration of HTTP server requests.", ServerSourceMeterName);

    public static readonly RequestDurationInstrument HttpClientRequests =
        new("http.client.request.duration", "Duration of HTTP client requests.", ClientSourceMeterName);

    private static readonly RequestDurationInstrument[] All = [HttpServerRequests, HttpClientRequests];

    private RequestDurationInstrument(string name, string description, string sourceMeterName)
    {
        Name = name;
        Description = description;
        SourceMeterName = sourceMeterName;
    }

    public string Name { get; }

    public string Description { get; }

    public string SourceMeterName { get; }

    public static bool IsAnySource(Instrument instrument)
    {
        return All.Any(requestDuration => requestDuration.IsSource(instrument));
    }

    public static bool IsAnyRelay(Instrument instrument)
    {
        return instrument.Meter.Name == RelayMeterName && All.Any(requestDuration => requestDuration.Name == instrument.Name);
    }

    public bool IsSource(Instrument instrument)
    {
        return instrument.Meter.Name == SourceMeterName && instrument.Name == Name;
    }
}
