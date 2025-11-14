using Api.Extensions; // Centralized DI + middleware registration for the API layer

var builder = WebApplication.CreateBuilder(args);

// Get the connection string (fail fast if required settings are missing)
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' not found.");

// Add Services
builder.Services.AddServices(builder.Configuration, connectionString);

var app = builder.Build();

// Add Application builders
app.AddApplicationBuilders();

await app.RunAsync();

public partial class Program { }
