using Api.Auth;
using Api.Endpoints.Health;
using Api.Errors;
using Application.Common.Abstractions.Auth;
using Infrastructure;
using Infrastructure.Data.Extensions;


var builder = WebApplication.CreateBuilder(args);

// --- Configuration ---

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' not found.");


// --- Services ---

builder.Services
    .AddInfrastructure(connectionString)
    .AddEndpointsApiExplorer()
    .AddSwaggerGen()
    .AddHttpContextAccessor()
    .AddScoped<ICurrentUserService, CurrentUserService>()
    .AddProblemDetailsAndExceptionMapping();

var app = builder.Build();


// --- Middleware  ---

app.UseGlobalExceptionHandling();
// app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// --- Endpoints ---

app.MapHealth();


// --- DB init (skip in tests or when disabled) ---

var isTesting = app.Environment.IsEnvironment("Testing");
var disableDbInit = Environment.GetEnvironmentVariable("DISABLE_DB_INIT") == "true";
if (!isTesting && !disableDbInit)
{
    await app.Services.ApplyMigrationsAndSeedAsync();
}

app.Run();

public partial class Program { }
