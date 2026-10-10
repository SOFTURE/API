using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SOFTURE.Common.Otlp.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IConfiguration? FindConfiguration(this IServiceCollection services)
    {
        var descriptor = services.LastOrDefault(x => x.ServiceType == typeof(IConfiguration));

        if (descriptor is null)
        {
            return null;
        }

        if (descriptor.ImplementationInstance is IConfiguration configuration)
        {
            return configuration;
        }

        return services.BuildServiceProvider().GetService<IConfiguration>();
    }
}
