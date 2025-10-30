using Api.Extensions; // Centralized DI + middleware registration for the API layer

var builder = WebApplication.CreateBuilder(args);

// 1) Configuration
// (Fail fast if required settings are missing)
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' not found.");

// 2) Services
builder.Services.AddApiLayer(builder.Configuration, connectionString);

var app = builder.Build();

// 3) Middleware pipeline
app.UseApiLayer();

// 4) Endpoints
app.MapApiEndpoints();
app.MapApiLayer(); // SignalR hub

await app.RunAsync();

public partial class Program { }
