using EventBus.Interfaces;
using EventBus.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace EventBus.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMQEventBus(
        this IServiceCollection services,
        string hostName = "localhost",
        string userName = "guest",
        string password = "guest")
    {
        services.AddSingleton<IConnectionFactory>(_ => new ConnectionFactory
        {
            HostName = hostName,
            UserName = userName,
            Password = password,
            DispatchConsumersAsync = true
        });

        services.AddSingleton<IEventBus, RabbitMQEventBus>();
        return services;
    }
}
