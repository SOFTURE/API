using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Shared between SOFTURE.Common.Logging and SOFTURE.Common.Observability (linked into both projects),
// so logs, traces and metrics resolve the same endpoint, headers and resource attributes from one
// configuration section. Every type here is internal to the assembly it is compiled into.
namespace SOFTURE.Common.Otlp;

internal enum OtlpTransport
{
    Grpc,
    HttpProtobuf
}

internal enum OtlpSignal
{
    Traces,
    Metrics,
    Logs
}

internal sealed record OtlpExportTarget(Uri Endpoint, OtlpTransport Protocol, string? Headers)
{
    public IDictionary<string, string> ParseHeaders() => OtlpConfiguration.ParseKeyValueList(Headers, "Headers");

    public bool Targets(Uri? requestUri) => OtlpConfiguration.IsSameOrigin(Endpoint, requestUri);
}

/// <summary>
/// Reads the <c>OpenTelemetry</c> configuration section and the standard <c>OTEL_*</c> environment variables.
/// Precedence per value: the <c>OpenTelemetry</c> section (any configuration provider, including
/// <c>OpenTelemetry__Endpoint</c>-style environment variables) wins, then the signal-specific
/// <c>OTEL_EXPORTER_OTLP_{SIGNAL}_*</c> variable, then the generic <c>OTEL_EXPORTER_OTLP_*</c> variable, then the default.
/// </summary>
internal sealed class OtlpConfiguration
{
    public const string SectionName = "OpenTelemetry";

    private const string DeploymentEnvironmentNameAttribute = "deployment.environment.name";
    private const string DeprecatedDeploymentEnvironmentAttribute = "deployment.environment";

    private readonly Func<string, string?> _getEnvironmentVariable;

    private OtlpConfiguration(Func<string, string?> getEnvironmentVariable)
    {
        _getEnvironmentVariable = getEnvironmentVariable;
    }

    public string? Endpoint { get; private init; }
    public OtlpTransport? Protocol { get; private init; }
    public string? Headers { get; private init; }
    public string? ServiceName { get; private init; }
    public string? ServiceNamespace { get; private init; }
    public string? ServiceVersion { get; private init; }
    public string? DeploymentEnvironment { get; private init; }
    public bool PrometheusEnabled { get; private init; } = true;

    public static OtlpConfiguration FromServices(IServiceCollection services) =>
        Read(FindConfiguration(services), Environment.GetEnvironmentVariable);

    public static OtlpConfiguration Read(IConfiguration? configuration, Func<string, string?> getEnvironmentVariable)
    {
        var section = configuration?.GetSection(SectionName);

        return new OtlpConfiguration(getEnvironmentVariable)
        {
            Endpoint = NullIfBlank(section?["Endpoint"]),
            Protocol = ParseProtocol(section?["Protocol"], $"{SectionName}:Protocol"),
            Headers = NullIfBlank(section?["Headers"]),
            ServiceName = NullIfBlank(section?["ServiceName"]),
            ServiceNamespace = NullIfBlank(section?["ServiceNamespace"]),
            ServiceVersion = NullIfBlank(section?["ServiceVersion"]),
            DeploymentEnvironment = NullIfBlank(section?["DeploymentEnvironment"]),
            PrometheusEnabled = ParseBoolean(section?["PrometheusEnabled"], $"{SectionName}:PrometheusEnabled") ?? true
        };
    }

    /// <summary>
    /// Resolves where one signal is exported. Returns <c>null</c> when neither the section nor the
    /// standard environment variables name an endpoint, which leaves the signal without an OTLP exporter.
    /// </summary>
    public OtlpExportTarget? ResolveExport(OtlpSignal signal)
    {
        var signalKey = signal.ToString().ToUpperInvariant();

        var protocol = Protocol
                       ?? ParseProtocol(Env($"OTEL_EXPORTER_OTLP_{signalKey}_PROTOCOL"), $"OTEL_EXPORTER_OTLP_{signalKey}_PROTOCOL")
                       ?? ParseProtocol(Env("OTEL_EXPORTER_OTLP_PROTOCOL"), "OTEL_EXPORTER_OTLP_PROTOCOL")
                       ?? OtlpTransport.HttpProtobuf;

        var headers = Headers
                      ?? Env($"OTEL_EXPORTER_OTLP_{signalKey}_HEADERS")
                      ?? Env("OTEL_EXPORTER_OTLP_HEADERS");

        Uri endpoint;
        if (Endpoint is not null)
        {
            endpoint = BuildSignalEndpoint(Endpoint, protocol, signal, $"{SectionName}:Endpoint");
        }
        else if (Env($"OTEL_EXPORTER_OTLP_{signalKey}_ENDPOINT") is { } signalEndpoint)
        {
            // Per the OpenTelemetry specification a signal-specific endpoint is used as-is.
            endpoint = ParseUri(signalEndpoint, $"OTEL_EXPORTER_OTLP_{signalKey}_ENDPOINT");
        }
        else if (Env("OTEL_EXPORTER_OTLP_ENDPOINT") is { } baseEndpoint)
        {
            endpoint = BuildSignalEndpoint(baseEndpoint, protocol, signal, "OTEL_EXPORTER_OTLP_ENDPOINT");
        }
        else
        {
            return null;
        }

        // Validates the header format at startup instead of on the first export.
        ParseKeyValueList(headers, "Headers");

        return new OtlpExportTarget(endpoint, protocol, headers);
    }

    /// <summary>
    /// The resource attributes shared by logs, traces and metrics. Starts from <c>OTEL_RESOURCE_ATTRIBUTES</c>
    /// and <c>OTEL_SERVICE_NAME</c>, lets the section override them and fills the defaults last.
    /// </summary>
    public IReadOnlyDictionary<string, object> ResolveResourceAttributes()
    {
        var attributes = new Dictionary<string, object>(StringComparer.Ordinal);

        foreach (var pair in ParseKeyValueList(Env("OTEL_RESOURCE_ATTRIBUTES"), "OTEL_RESOURCE_ATTRIBUTES"))
            attributes[pair.Key] = pair.Value;

        if (Env("OTEL_SERVICE_NAME") is { } environmentServiceName)
            attributes["service.name"] = environmentServiceName;

        SetIfPresent(attributes, "service.name", ServiceName);
        SetIfPresent(attributes, "service.namespace", ServiceNamespace);
        SetIfPresent(attributes, "service.version", ServiceVersion);

        var deploymentEnvironment = DeploymentEnvironment
                                    ?? GetString(attributes, DeploymentEnvironmentNameAttribute)
                                    ?? GetString(attributes, DeprecatedDeploymentEnvironmentAttribute)
                                    ?? Env("ASPNETCORE_ENVIRONMENT")
                                    ?? Env("DOTNET_ENVIRONMENT");

        if (deploymentEnvironment is not null)
        {
            attributes[DeploymentEnvironmentNameAttribute] = deploymentEnvironment;
            attributes[DeprecatedDeploymentEnvironmentAttribute] = deploymentEnvironment;
        }

        var entryAssembly = Assembly.GetEntryAssembly();

        if (!attributes.ContainsKey("service.name"))
            attributes["service.name"] = entryAssembly?.GetName().Name ?? "Unknown";

        if (!attributes.ContainsKey("service.version") && EntryAssemblyVersion(entryAssembly) is { } version)
            attributes["service.version"] = version;

        if (!attributes.ContainsKey("service.instance.id"))
            attributes["service.instance.id"] = Environment.MachineName;

        return attributes;
    }

    public static IDictionary<string, string> ParseKeyValueList(string? value, string source)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(value))
            return result;

        foreach (var member in value!.Split(','))
        {
            if (string.IsNullOrWhiteSpace(member))
                continue;

            var separator = member.IndexOf('=');
            var key = separator > 0 ? member.Substring(0, separator).Trim() : string.Empty;

            if (key.Length == 0)
                throw new InvalidOperationException(
                    $"Invalid entry '{member.Trim()}' in {source}: expected the 'key=value,key2=value2' format.");

            result[key] = Uri.UnescapeDataString(member.Substring(separator + 1).Trim());
        }

        return result;
    }

    public static bool IsSameOrigin(Uri endpoint, Uri? requestUri) =>
        requestUri is not null
        && requestUri.IsAbsoluteUri
        && string.Equals(requestUri.Scheme, endpoint.Scheme, StringComparison.OrdinalIgnoreCase)
        && string.Equals(requestUri.Host, endpoint.Host, StringComparison.OrdinalIgnoreCase)
        && requestUri.Port == endpoint.Port;

    public static Uri BuildSignalEndpoint(string baseEndpoint, OtlpTransport protocol, OtlpSignal signal, string source)
    {
        var endpoint = ParseUri(baseEndpoint, source);

        if (protocol == OtlpTransport.Grpc)
            return endpoint;

        var signalPath = "/v1/" + signal.ToString().ToLowerInvariant();
        var basePath = endpoint.AbsolutePath.TrimEnd('/');

        if (basePath.EndsWith(signalPath, StringComparison.OrdinalIgnoreCase))
            return endpoint;

        return new UriBuilder(endpoint) { Path = basePath + signalPath }.Uri;
    }

    private static IConfiguration? FindConfiguration(IServiceCollection services)
    {
        var descriptor = services.LastOrDefault(x => x.ServiceType == typeof(IConfiguration));

        if (descriptor is null)
            return null;

        if (descriptor.ImplementationInstance is IConfiguration configuration)
            return configuration;

        return services.BuildServiceProvider().GetService<IConfiguration>();
    }

    private string? Env(string name) => NullIfBlank(_getEnvironmentVariable(name));

    private static string? EntryAssemblyVersion(Assembly? assembly)
    {
        var version = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                      ?? assembly?.GetName().Version?.ToString();

        if (version is null)
            return null;

        var metadataSeparator = version.IndexOf('+');
        return metadataSeparator > 0 ? version.Substring(0, metadataSeparator) : version;
    }

    private static Uri ParseUri(string value, string source) =>
        Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
            ? uri
            : throw new InvalidOperationException($"Invalid OTLP endpoint '{value}' in {source}: expected an absolute URL.");

    private static OtlpTransport? ParseProtocol(string? value, string source)
    {
        var normalized = NullIfBlank(value)?.Replace("/", string.Empty).Replace("-", string.Empty).ToLowerInvariant();

        return normalized switch
        {
            null => null,
            "grpc" => OtlpTransport.Grpc,
            "httpprotobuf" => OtlpTransport.HttpProtobuf,
            _ => throw new InvalidOperationException(
                $"Unsupported OTLP protocol '{value}' in {source}: use 'HttpProtobuf' (http/protobuf) or 'Grpc'.")
        };
    }

    private static bool? ParseBoolean(string? value, string source)
    {
        if (NullIfBlank(value) is not { } text)
            return null;

        return bool.TryParse(text, out var result)
            ? result
            : throw new InvalidOperationException($"Invalid boolean '{value}' in {source}.");
    }

    private static void SetIfPresent(IDictionary<string, object> attributes, string key, string? value)
    {
        if (value is not null)
            attributes[key] = value;
    }

    private static string? GetString(IDictionary<string, object> attributes, string key) =>
        attributes.TryGetValue(key, out var value) ? NullIfBlank(value as string) : null;

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value!.Trim();
}
