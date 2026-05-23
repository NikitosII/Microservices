using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventBus.Extensions
{
    /// <summary>
    /// Extension methods that wire up the shared MassTransit/RabbitMQ event bus.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers MassTransit with a RabbitMQ transport.
        /// The optional <paramref name="configure"/> delegate lets each service add its own consumers
        /// before the bus is built (e.g. <c>x.AddConsumer&lt;MyConsumer&gt;()</c>).
        /// Endpoint names use kebab-case derived from the consumer class name.
        /// </summary>
        public static IServiceCollection AddEventBus(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<IBusRegistrationConfigurator>? configure = null)
        {
            services.AddMassTransit(x =>
            {
                configure?.Invoke(x);

                x.SetKebabCaseEndpointNameFormatter();

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(configuration["RabbitMQ:Host"], h =>
                    {
                        h.Username(configuration["RabbitMQ:Username"]);
                        h.Password(configuration["RabbitMQ:Password"]);
                    });

                    cfg.ConfigureEndpoints(context);
                });
            });

            return services;
        }
    }
}