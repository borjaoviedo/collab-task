using Api.Auth;
using Application.Common.Abstractions.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

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

app.Run();
