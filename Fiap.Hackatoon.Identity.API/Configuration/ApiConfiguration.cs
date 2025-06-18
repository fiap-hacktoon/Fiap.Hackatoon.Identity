using Fiap.Hackatoon.Identity.Domain.DTOs;
using Fiap.Hackatoon.Identity.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Fiap.Hackatoon.Identity.API.Configuration
{
    public static class ApiConfiguration
    {
        public static void AddApiConfiguration(this IServiceCollection services, IConfiguration configuration)
        {            
            services.AddDbContext<IdentityContext>(options =>
              options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

            services.AddEndpointsApiExplorer();

            services.UseHttpClientMetrics();

            services.Configure<RabbitMqConnection>(configuration.GetSection("RabbitMq"));

            services.AddRabitMqConfiguration(configuration);
        }

        public static void UseApiConfiguration(this WebApplication app, IWebHostEnvironment env)
        {      
            app.UseHealthChecks("/health");
            app.UseMetricServer();
            app.UseHttpMetrics();
            //app.UseHttpsRedirection();            
            app.MapControllers();
            app.UseAuthConfiguration();
        }
    }
}
