using System.Reflection;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using SOFTURE.Common.HealthCheck;
using SOFTURE.Contract.Common.Messaging;
using SOFTURE.MessageBroker.Rabbit.Configuration;
using SOFTURE.MessageBroker.Rabbit.Filters;
using SOFTURE.MessageBroker.Rabbit.HealthChecks;
using SOFTURE.MessageBroker.Rabbit.Settings;

namespace SOFTURE.MessageBroker.Rabbit;

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

                cfg.Host(publisherSettings.Url, ConfigureHost);
            });
        });

        return services;
    }

    public static IServiceCollection AddCommonConsumers<TSettings>(
        this IServiceCollection services,
        Assembly assembly,
        Action<RabbitConsumersOptions>? configure = null)
        where TSettings : IRabbitSettings
    {
        var options = new RabbitConsumersOptions();

        configure?.Invoke(options);

        options.Validate();

        var allConsumerTypes = GetAllConsumers(assembly);

        var assignment = ConsumerAssignment.Create(allConsumerTypes, options);

        services.AddMassTransit(config =>
        {
            foreach (var consumerType in allConsumerTypes)
                config.AddConsumer(consumerType);

            config.UsingRabbitMq((ctx, cfg) =>
            {
                var consumerSettings = ctx.GetRequiredService<TSettings>().Rabbit;

                cfg.UseInMemoryOutbox(ctx);

                cfg.UseConsumeFilter(typeof(ContextConsumeLoggingFilter<>), ctx);

                cfg.Host(consumerSettings.Url, ConfigureHost);

                ConfigureEndpoint(cfg, ctx, consumerSettings.Name, options.Default, assignment.Default);

                foreach (var group in options.Groups)
                {
                    ConfigureEndpoint(cfg, ctx, ConsumerAssignment.GetEndpointName(consumerSettings.Name, group.Name), group, assignment.Groups[group.Name]);
                }
            });
        });

        services.AddCommonHealthCheck<MessageBrokerHealthCheck>();

        return services;
    }

    private static void ConfigureEndpoint(
        IRabbitMqBusFactoryConfigurator cfg,
        IBusRegistrationContext ctx,
        string endpointName,
        ConsumerEndpointOptions options,
        IReadOnlyCollection<Type> consumerTypes)
    {
        if (consumerTypes.Count == 0)
            return;

        cfg.ReceiveEndpoint(endpointName, endpoint =>
        {
            endpoint.PrefetchCount = options.PrefetchCount;

            if (options.ConcurrentMessageLimit is { } concurrentMessageLimit)
                endpoint.ConcurrentMessageLimit = concurrentMessageLimit;

            if (options.KillSwitch is { } killSwitch)
            {
                endpoint.UseKillSwitch(k => k
                    .SetActivationThreshold(killSwitch.ActivationThreshold)
                    .SetTripThreshold(killSwitch.TripThreshold)
                    .SetTrackingPeriod(killSwitch.TrackingPeriod)
                    .SetRestartTimeout(killSwitch.RestartTimeout));
            }

            if (options.RateLimit is { } rateLimit)
                endpoint.UseRateLimit(rateLimit.Limit, rateLimit.Interval);

            if (options.ConsumeTimeout is { } consumeTimeout)
                endpoint.UseTimeout(t => t.Timeout = consumeTimeout);

            ConfigureRetry(endpoint, options);

            foreach (var consumerType in consumerTypes)
                endpoint.ConfigureConsumer(ctx, consumerType);
        });
    }

    private static void ConfigureRetry(IReceiveEndpointConfigurator endpoint, ConsumerEndpointOptions options)
    {
        if (options.ConfigureRetry is { } configureRetry)
        {
            endpoint.UseMessageRetry(configureRetry);

            return;
        }

        if (options.Retry is not { Count: > 0 } retry)
            return;

        endpoint.UseMessageRetry(r =>
        {
            foreach (var ignoredException in retry.IgnoredExceptions)
                r.Ignore(ignoredException);

            if (retry.IsExponential)
                r.Exponential(retry.Count, retry.MinInterval, retry.MaxInterval, retry.IntervalDelta);
            else
                r.Immediate(retry.Count);
        });
    }

    private static void ConfigureHost(IRabbitMqHostConfigurator host)
    {
#if NET6_0
        host.ConfigureBatchPublish(bcfg =>
        {
            bcfg.Enabled = true;
            bcfg.MessageLimit = 100;
            bcfg.SizeLimit = 10000;
            bcfg.Timeout = TimeSpan.FromMilliseconds(30);
        });
#endif
    }

    private static List<Type> GetAllConsumers(Assembly assembly)
    {
        return GetConsumers<IMessage>(assembly)
            .Concat(GetConsumers<IBulkMessage>(assembly))
            .Distinct()
            .ToList();
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
