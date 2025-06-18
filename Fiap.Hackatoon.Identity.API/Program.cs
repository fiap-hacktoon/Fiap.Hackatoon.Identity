using Fiap.Hackatoon.Identity.API.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
builder.Services.AddApiConfiguration(builder.Configuration);

builder.Services.AddJwtConfiguration(builder.Configuration);
builder.Services.RegisterServices();
builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddSwaggerConfiguration();

var app = builder.Build();

app.UseApiConfiguration(app.Environment);
app.UseSwaggerConfiguration();

app.Run();

public partial class Program { }
