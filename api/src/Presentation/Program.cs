using Api.Auth;
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

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
   .WithTags("Health")
   .WithName("Health")
   .WithSummary("Health check")
   .Produces(StatusCodes.Status200OK);

await app.Services.ApplyMigrationsAndSeedAsync();

app.Run();

public partial class Program { }
