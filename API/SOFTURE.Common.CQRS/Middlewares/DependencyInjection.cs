using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SOFTURE.Common.CQRS.Middlewares.Behaviours;

namespace SOFTURE.Common.CQRS.Middlewares
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddMiddlewares(this IServiceCollection services)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CommandValidationBehaviour<,>));

            return services;
        }
    }
}
