using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Krizovka.Shared.Messaging;

public static class MessagingExtensions
{
    public static IServiceCollection AddSharedMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureConsumers = null)
    {
        services.AddMassTransit(x =>
        {
            // Allow services to register their specific consumers
            configureConsumers?.Invoke(x);

            var host = Environment.GetEnvironmentVariable("RABBITMQ_HOST")
                       ?? configuration.GetValue<string>("RabbitMQ:Host");

            var user = Environment.GetEnvironmentVariable("RABBITMQ_USER")
                       ?? configuration.GetValue<string>("RabbitMQ:User");
            var pass = Environment.GetEnvironmentVariable("RABBITMQ_PASS")
                       ?? configuration.GetValue<string>("RabbitMQ:Password");

            if (host == "in-memory" || Environment.GetEnvironmentVariable("MASSTRANSIT_INMEMORY") == "true" || string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
            {
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
                    cfg.ConfigureEndpoints(context);
                });
            }
            else
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    var portStr = Environment.GetEnvironmentVariable("RABBITMQ_PORT")
                                  ?? configuration.GetValue<string>("RabbitMQ:Port") ?? "5672";

                    if (!ushort.TryParse(portStr, out var port))
                    {
                        port = 5672;
                    }

                    cfg.Host(host, port, "/", h =>
                    {
                        h.Username(user);
                        h.Password(pass);
                    });

                    cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
                    cfg.ConfigureEndpoints(context);
                });
            }
        });

        return services;
    }
}
