using System.Reflection;
using DataViz.BuildingBlocks.Infrastructure.Messaging;
using DataViz.BuildingBlocks.Infrastructure.Outbox;
using DataViz.BuildingBlocks.Infrastructure.Persistence;
using DataViz.BuildingBlocks.Messaging;
using DataViz.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DataViz.BuildingBlocks.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Connects the service to RabbitMQ. Integration event handlers found in the application
    /// assembly are subscribed automatically; a service without handlers only publishes.
    /// </summary>
    public static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        Assembly applicationAssembly)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<IMessageBroker, RabbitMqMessageBroker>();
        services.AddSingleton(services.SubscribeIntegrationEventHandlers(applicationAssembly));
        services.AddHostedService<RabbitMqConsumer>();

        return services;
    }

    /// <summary>
    /// Wires the unit of work and the transactional outbox for a write model stored in
    /// <typeparamref name="TContext"/>.
    /// </summary>
    public static IServiceCollection AddTransactionalOutbox<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddScoped<IUnitOfWork, UnitOfWork<TContext>>();
        services.AddScoped<IIntegrationEventOutbox, EfIntegrationEventOutbox<TContext>>();
        services.AddHostedService<OutboxPublisher<TContext>>();

        return services;
    }

    private static EventSubscriptions SubscribeIntegrationEventHandlers(
        this IServiceCollection services,
        Assembly applicationAssembly)
    {
        var subscriptions = new EventSubscriptions();
        var subscribe = typeof(EventSubscriptions).GetMethod(nameof(EventSubscriptions.Add))!;

        var handlers =
            from implementation in applicationAssembly.GetTypes()
            where implementation is { IsClass: true, IsAbstract: false }
            from contract in implementation.GetInterfaces()
            where contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>)
            select (contract, implementation);

        foreach (var (contract, implementation) in handlers)
        {
            services.AddScoped(contract, implementation);
            subscribe.MakeGenericMethod(contract.GetGenericArguments()[0]).Invoke(subscriptions, null);
        }

        return subscriptions;
    }
}
