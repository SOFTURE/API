using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using SOFTURE.Common.HealthCheck.Core;
using SOFTURE.Common.HealthCheck.Presentation;

namespace SOFTURE.Common.HealthCheck
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddCommonHealthCheck<THealthCheck>(this IServiceCollection services)
            where THealthCheck : CheckBase, ICommonHealthCheck
        {
            var healthCheckName = typeof(THealthCheck).Name.Replace("HealthCheck", string.Empty);
            
            services.AddHealthChecks()
                .AddCheck<THealthCheck>(healthCheckName, tags: new[] { Consts.HealthCheckTag });
            
            return services;
        }

        public static WebApplication MapCommonHealthChecks(this WebApplication app)
        {
            app.MapHealthChecks("/hc", new HealthCheckOptions
            {
                ResponseWriter = Writer.WriteResponse,
                Predicate = healthCheck => healthCheck.Tags.Contains(Consts.HealthCheckTag)
            });

            return app;
        }
    }
}