using System.Reflection;

namespace SOFTURE.Common.Otlp.Extensions;

internal static class AssemblyExtensions
{
    public static string? GetServiceVersion(this Assembly? assembly)
    {
        var version = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                      ?? assembly?.GetName().Version?.ToString();

        return version?.WithoutBuildMetadata();
    }
}
