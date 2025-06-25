using Fiap.Hackatoon.Identity.Application.Applications;
using Fiap.Hackatoon.Identity.Domain.DTOs;
using MassTransit;
namespace Fiap.Hackatoon.Identity.API.Configuration
{
    public static class RabbitMqConfiguration
    {
        public static void AddRabitMqConfiguration(this IServiceCollection services, IConfiguration configuration)
        {

            var rabbitMqConfig = configuration.GetSection("RabbitMq").Get<RabbitMqConnection>();

            services.AddMassTransit(x =>
            {
                x.AddConsumer<DummyConsumer>();
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(new Uri($"amqp://{rabbitMqConfig.HostName}:{rabbitMqConfig.Port}"), h => {
                        h.Username(rabbitMqConfig.UserName);
                        h.Password(rabbitMqConfig.Password);
                    });

                    cfg.ConfigureEndpoints(context);
                });
            });

        }
    }
}
