using System.Reflection;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using SOFTURE.Common.HealthCheck;
using SOFTURE.Contract.Common.Messaging;
using SOFTURE.MessageBroker.Rabbit.Filters;
using SOFTURE.MessageBroker.Rabbit.HealthChecks;
using SOFTURE.MessageBroker.Rabbit.Settings;

namespace SOFTURE.MessageBroker.Rabbit
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddCommonPublisher<TSettings>(this IServiceCollection services)
            where TSettings : IRabbitSettings
        {
            services.AddMassTransit(config =>
            {
                config.UsingRabbitMq((ctx, cfg) =>
                {
                    var publisherSettings = ctx.GetRequiredService<TSettings>().Rabbit;

                    cfg.UseInMemoryOutbox(ctx);

                    cfg.UsePublishFilter(typeof(ContextPublishLoggingFilter<>), ctx);

                    cfg.Host(publisherSettings.Url, h =>
                    {
                        h.ConfigureBatchPublish(bcfg =>
                        {
                            bcfg.Enabled = true;
                            bcfg.MessageLimit = 100;
                            bcfg.SizeLimit = 10000;
                            bcfg.Timeout = TimeSpan.FromMilliseconds(30);
                        });
                    });
                });
            });

            return services;
        }

        public static IServiceCollection AddCommonConsumers<TSettings>(
            this IServiceCollection services,
            Assembly assembly,
            int retryCount = 0)
            where TSettings : IRabbitSettings
        {
            var consumerTypes = GetConsumers<IMessage>(assembly);
            var bulkConsumerTypes = GetConsumers<IBulkMessage>(assembly);

            var allConsumerTypes = consumerTypes
                .Concat(bulkConsumerTypes)
                .ToList();

            services.AddMassTransit(config =>
            {
                foreach (var type in allConsumerTypes)
                {
                    config.AddConsumer(type);
                }

                config.UsingRabbitMq((ctx, cfg) =>
                {
                    var consumerSettings = ctx.GetRequiredService<TSettings>().Rabbit;

                    cfg.UseInMemoryOutbox(ctx);

                    cfg.UseConsumeFilter(typeof(ContextConsumeLoggingFilter<>), ctx);

                    cfg.Host(consumerSettings.Url, h =>
                    {
                        h.ConfigureBatchPublish(bcfg =>
                        {
                            bcfg.Enabled = true;
                            bcfg.MessageLimit = 100;
                            bcfg.SizeLimit = 10000;
                            bcfg.Timeout = TimeSpan.FromMilliseconds(30);
                        });
                    });

                    cfg.ReceiveEndpoint(consumerSettings.Name, c =>
                    {
                        foreach (var type in consumerTypes)
                        {
                            c.ConfigureConsumer(ctx, type);
                            c.PrefetchCount = 1;

                            if (retryCount != 0)
                                c.UseMessageRetry(r => r.Immediate(retryCount));
                        }

                        foreach (var type in bulkConsumerTypes)
                        {
                            c.ConfigureConsumer(ctx, type);
                            c.PrefetchCount = 50;

                            if (retryCount != 0)
                                c.UseMessageRetry(r => r.Immediate(retryCount));
                        }
                    });
                });
            });

            services.AddCommonHealthCheck<MessageBrokerHealthCheck>();

            return services;
        }

        private static List<Type> GetConsumers<T>(Assembly assembly) where T : class
        {
            var consumerTypes = assembly.GetTypes()
                .Where(t =>
                    t.GetInterfaces().Any(i =>
                        i.IsGenericType &&
                        i.GetGenericTypeDefinition() == typeof(IConsumer<>) &&
                        typeof(T).IsAssignableFrom(i.GetGenericArguments()[0])) &&
                    !t.IsAbstract)
                .ToList();

            return consumerTypes;
        }
    }
}