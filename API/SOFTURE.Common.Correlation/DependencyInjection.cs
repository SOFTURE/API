using Microsoft.Extensions.DependencyInjection;
using SOFTURE.Common.Correlation.Providers;

namespace SOFTURE.Common.Correlation
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddCommonCorrelationProvider(this IServiceCollection services)
        {
            services.AddScoped<ICorrelationProvider, CorrelationProvider>();
            
            return services;
        }
    }
}