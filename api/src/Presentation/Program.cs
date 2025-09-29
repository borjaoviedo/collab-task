using Api.Auth;
using Api.Endpoints.Auth;
using Api.Endpoints.Health;
using Api.Errors;
using Api.Extensions;
using Application.Common.Abstractions.Auth;
using Infrastructure;


var builder = WebApplication.CreateBuilder(args);

// --- Configuration ---

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' not found.");

builder.Services
    .AddInfrastructure(connectionString)
    .AddSwaggerWithJwt()
    .AddJwtAuth(builder.Configuration)
    .AddHttpContextAccessor()
    .AddScoped<ICurrentUserService, CurrentUserService>()
    .AddAppValidation()
    .AddProblemDetailsAndExceptionMapping();

var app = builder.Build();


// --- Middleware  ---

app.UseGlobalExceptionHandling();
app.UseAuthentication();
app.UseAuthorization();
app.UseSwaggerUiIfDev(app.Environment);


// --- Endpoints ---

app.MapHealth();
app.MapAuth();

app.Run();

public partial class Program { }
