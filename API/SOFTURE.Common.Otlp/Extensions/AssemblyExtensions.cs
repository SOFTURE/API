using System.Reflection;

namespace SOFTURE.Common.Otlp.Extensions;

public static class AssemblyExtensions
{
    public static string GetServiceVersion(this Assembly assembly)
    {
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                      ?? assembly.GetName().Version?.ToString()
                      ?? "0.0.0";

        return version.WithoutBuildMetadata();
    }
}
