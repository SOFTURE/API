using SOFTURE.Common.Otlp.Models;

namespace SOFTURE.Common.Otlp.Extensions;

internal static class OtlpConfigurationExtensions
{
    public static OtlpTransport ResolveProtocol(this OtlpConfiguration configuration, OtlpSignal signal)
    {
        var signalVariable = signal.ToEnvironmentVariable("PROTOCOL");

        return configuration.Protocol
               ?? configuration.GetEnvironmentVariable(signalVariable).ToOtlpTransport(signalVariable)
               ?? configuration.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL").ToOtlpTransport("OTEL_EXPORTER_OTLP_PROTOCOL")
               ?? OtlpTransport.HttpProtobuf;
    }

    public static string? ResolveHeaders(this OtlpConfiguration configuration, OtlpSignal signal) =>
        (configuration.Headers
         ?? configuration.GetEnvironmentVariable(signal.ToEnvironmentVariable("HEADERS"))
         ?? configuration.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS"))
        .EnsureValidKeyValueList("Headers");

    public static Uri? ResolveEndpoint(this OtlpConfiguration configuration, OtlpSignal signal, OtlpTransport protocol)
    {
        if (configuration.Endpoint is { } sectionEndpoint)
        {
            return sectionEndpoint
                .ToAbsoluteUri($"{OtlpConfiguration.SectionName}:Endpoint")
                .WithSignalPath(protocol, signal);
        }

        var signalVariable = signal.ToEnvironmentVariable("ENDPOINT");

        if (configuration.GetEnvironmentVariable(signalVariable) is { } signalEndpoint)
        {
            return signalEndpoint.ToAbsoluteUri(signalVariable);
        }

        if (configuration.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") is { } baseEndpoint)
        {
            return baseEndpoint
                .ToAbsoluteUri("OTEL_EXPORTER_OTLP_ENDPOINT")
                .WithSignalPath(protocol, signal);
        }

        return null;
    }

    private static string ToEnvironmentVariable(this OtlpSignal signal, string setting) =>
        $"OTEL_EXPORTER_OTLP_{signal.ToString().ToUpperInvariant()}_{setting}";
}
