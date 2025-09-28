using Api.Auth;
using Api.Endpoints.Health;
using Application.Common.Abstractions.Auth;
using Infrastructure;
using Infrastructure.Data.Extensions;


var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' not found.");

builder.Services
    .AddInfrastructure(connectionString)
    .AddEndpointsApiExplorer()
    .AddSwaggerGen()
    .AddProblemDetails()
    .AddHttpContextAccessor()
    .AddScoped<ICurrentUserService, CurrentUserService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
// app.UseHttpsRedirection();

app.MapHealth();

var isTesting = app.Environment.IsEnvironment("Testing");
var disableDbInit = Environment.GetEnvironmentVariable("DISABLE_DB_INIT") == "true";

if (!isTesting && !disableDbInit)
{
    await app.Services.ApplyMigrationsAndSeedAsync();
}

app.Run();

public partial class Program { }
