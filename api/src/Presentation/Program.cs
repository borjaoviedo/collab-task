using Api.Auth;
using Api.Endpoints.Auth;
using Api.Endpoints.Health;
using Api.Errors;
using Api.Extensions;
using Application.Common.Abstractions.Auth;
using Infrastructure;
using Infrastructure.Data.Extensions;
using Infrastructure.Security;


var builder = WebApplication.CreateBuilder(args);


// --- Configuration ---

if (builder.Environment.IsDevelopment())
    builder.Configuration.AddUserSecrets<Program>();

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' not found.");

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.Configure<JwtOptions>(o =>
    {
        o.Issuer = "Test";
        o.Audience = "Test";
        o.Key = new string('k', 32);
        o.ExpMinutes = 60;
    });
}
else
{
    builder.Services
        .AddOptions<JwtOptions>()
        .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
        .Validate(o => !string.IsNullOrWhiteSpace(o.Key) && o.Key.Length >= 32, "Jwt:Key must be at least 32 chars.")
        .ValidateOnStart();
}


// --- Services ---

builder.Services
    .AddAppValidation()
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
app.MapAuth();

// --- DB init (skip in tests or when disabled) ---

var isTesting = app.Environment.IsEnvironment("Testing");
var disableDbInit = Environment.GetEnvironmentVariable("DISABLE_DB_INIT") == "true";
if (!isTesting && !disableDbInit)
{
    await app.Services.ApplyMigrationsAndSeedAsync();
}

app.Run();

public partial class Program { }
