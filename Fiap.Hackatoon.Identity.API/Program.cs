using Fiap.Hackatoon.Identity.API.Configuration;
using Fiap.Hackatoon.Identity.Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
builder.Services.AddApiConfiguration(builder.Configuration);

if (builder.Environment.EnvironmentName != "IntegrationTesting")
{
    builder.Services.AddRabitMqConfiguration(builder.Configuration);
}
else
{
    Console.WriteLine("Aplicação rodando em ambiente 'IntegrationTesting'. MassTransit REAL desativado.");
}


builder.Services.AddJwtConfiguration(builder.Configuration);
builder.Services.RegisterServices();
builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddSwaggerConfiguration();

var app = builder.Build();


app.UseApiConfiguration(app.Environment);


if (builder.Environment.EnvironmentName != "IntegrationTesting")
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<IdentityContext>();

        context.Database.Migrate();
    }

}


app.UseSwaggerConfiguration();

app.Run();

public partial class Program { }


