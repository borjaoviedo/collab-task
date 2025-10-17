using Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' not found.");

builder.Services.AddApiLayer(builder.Configuration, connectionString);

var app = builder.Build();

app.UseApiLayer();
app.MapApiEndpoints();
app.MapApiLayer();

app.Run();

public partial class Program { }
