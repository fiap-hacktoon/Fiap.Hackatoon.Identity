using Fiap.Hackatoon.Identity.API.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiConfiguration(builder.Configuration);
builder.Services.RegisterServices();
builder.Services.AddAutoMapper(typeof(Program));

var app = builder.Build();

app.UseApiConfiguration(app.Environment);

app.Run();

public partial class Program { }
