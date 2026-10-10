using System.Reflection;
using SOFTURE.Common.Otlp.Models;
using SOFTURE.Common.Otlp.Settings;

namespace SOFTURE.Common.Otlp.Extensions;

public static class OpenTelemetrySettingsExtensions
{
    public static Uri GetEndpoint(this OpenTelemetrySettings settings) =>
        Uri.TryCreate(settings.Endpoint.Trim(), UriKind.Absolute, out var endpoint)
            ? endpoint
            : throw new InvalidOperationException(
                $"Invalid OpenTelemetry:Endpoint '{settings.Endpoint}': expected an absolute URL.");

    public static Uri GetSignalEndpoint(this OpenTelemetrySettings settings, OtlpSignal signal) =>
        settings.GetEndpoint().WithPathSuffix(signal.ToHttpPath());

    public static IDictionary<string, string> GetHeaders(this OpenTelemetrySettings settings) =>
        settings.Headers.ToKeyValuePairs();

    public static bool IsExportRequest(this OpenTelemetrySettings settings, Uri? requestUri) =>
        settings.GetEndpoint().IsSameOrigin(requestUri);

    public static IReadOnlyDictionary<string, object> GetResourceAttributes(this OpenTelemetrySettings settings)
    {
        var entryAssembly = Assembly.GetEntryAssembly();

        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["service.name"] = settings.ServiceName,
            ["service.namespace"] = settings.ServiceNamespace,
            ["service.version"] = entryAssembly?.GetServiceVersion() ?? "0.0.0",
            ["service.instance.id"] = Environment.MachineName,
            ["deployment.environment.name"] = settings.Environment,
            ["deployment.environment"] = settings.Environment
        };
    }
}
