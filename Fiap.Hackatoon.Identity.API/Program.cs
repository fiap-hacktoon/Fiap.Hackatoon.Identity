using Fiap.Hackatoon.Identity.API.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiConfiguration(builder.Configuration);

var app = builder.Build();

app.UseApiConfiguration(app.Environment);

app.Run();

public partial class Program { }
