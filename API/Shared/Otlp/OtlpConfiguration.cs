using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SOFTURE.Common.Otlp.Extensions;
using SOFTURE.Common.Otlp.Models;

namespace SOFTURE.Common.Otlp;

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

    public static OtlpConfiguration FromServices(IServiceCollection services) => Read(services.FindConfiguration(), Environment.GetEnvironmentVariable);

    public static OtlpConfiguration Read(IConfiguration? configuration, Func<string, string?> getEnvironmentVariable)
    {
        var section = configuration?.GetSection(SectionName);

        return new OtlpConfiguration(getEnvironmentVariable)
        {
            Endpoint = section?["Endpoint"].NullIfBlank(),
            Protocol = section?["Protocol"].ToOtlpTransport($"{SectionName}:Protocol"),
            Headers = section?["Headers"].NullIfBlank(),
            ServiceName = section?["ServiceName"].NullIfBlank(),
            ServiceNamespace = section?["ServiceNamespace"].NullIfBlank(),
            ServiceVersion = section?["ServiceVersion"].NullIfBlank(),
            DeploymentEnvironment = section?["DeploymentEnvironment"].NullIfBlank(),
            PrometheusEnabled = section?["PrometheusEnabled"].ToBoolean($"{SectionName}:PrometheusEnabled") ?? true
        };
    }

    public string? GetEnvironmentVariable(string name) => _getEnvironmentVariable(name).NullIfBlank();

    public OtlpExportTarget? ResolveExport(OtlpSignal signal)
    {
        var protocol = this.ResolveProtocol(signal);
        var endpoint = this.ResolveEndpoint(signal, protocol);

        return endpoint is null ? null : new OtlpExportTarget(endpoint, protocol, this.ResolveHeaders(signal));
    }

    public IReadOnlyDictionary<string, object> ResolveResourceAttributes()
    {
        var attributes = new Dictionary<string, object>(StringComparer.Ordinal);

        foreach (var pair in GetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES").ToKeyValuePairs("OTEL_RESOURCE_ATTRIBUTES"))
        {
            attributes[pair.Key] = pair.Value;
        }

        attributes.SetIfPresent("service.name", GetEnvironmentVariable("OTEL_SERVICE_NAME"));
        attributes.SetIfPresent("service.name", ServiceName);
        attributes.SetIfPresent("service.namespace", ServiceNamespace);
        attributes.SetIfPresent("service.version", ServiceVersion);

        var deploymentEnvironment = DeploymentEnvironment
                                    ?? attributes.GetNonBlankString(DeploymentEnvironmentNameAttribute)
                                    ?? attributes.GetNonBlankString(DeprecatedDeploymentEnvironmentAttribute)
                                    ?? GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                                    ?? GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        attributes.SetIfPresent(DeploymentEnvironmentNameAttribute, deploymentEnvironment);
        attributes.SetIfPresent(DeprecatedDeploymentEnvironmentAttribute, deploymentEnvironment);

        var entryAssembly = Assembly.GetEntryAssembly();

        attributes.SetIfMissing("service.name", entryAssembly?.GetName().Name ?? "Unknown");
        attributes.SetIfMissing("service.version", entryAssembly.GetServiceVersion());
        attributes.SetIfMissing("service.instance.id", Environment.MachineName);

        return attributes;
    }
}
