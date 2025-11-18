using Api.Filters;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Api.Tests.Testing
{
    public sealed class TestApiFactory : WebApplicationFactory<Program>
    {
        private SqliteConnection? _connection;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((ctx, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] = "Test",
                    ["Jwt:Audience"] = "Test",
                    ["Jwt:Key"] = new string('k', 32),
                    ["Jwt:ExpMinutes"] = "60"
                });
            });

            builder.ConfigureServices(services =>
            {
                // Core test services
                services.AddHttpContextAccessor();
                services.AddProblemDetails();

                // Hard reset of any EF registrations that AddInfrastructure made
                services.RemoveAll<DbContextOptions<CollabTaskDbContext>>();
                services.RemoveAll<CollabTaskDbContext>();
                services.RemoveAll<IDbContextFactory<CollabTaskDbContext>>();
                services.RemoveAll<PooledDbContextFactory<CollabTaskDbContext>>();
                services.RemoveAll<IConfigureOptions<DbContextOptions<CollabTaskDbContext>>>();
                services.RemoveAll<IPostConfigureOptions<DbContextOptions<CollabTaskDbContext>>>();

                // Single provider: SQLite in-memory with a kept-open connection
                _connection = new SqliteConnection("Data Source=:memory:");
                _connection.Open();

                // Register the DbContext via factory so it uses a FRESH options instance with ONLY Sqlite
                services.AddScoped(sp =>
                {
                    var options = new DbContextOptionsBuilder<CollabTaskDbContext>()
                        .UseSqlite(_connection)
                        .Options;

                    return new CollabTaskDbContext(options);
                });

                // Ensure schema without migrations
                using var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CollabTaskDbContext>();
                db.Database.EnsureCreated();

                // Filters / utilities
                services.TryAddScoped<IfMatchRowVersionFilter>();
            });
        }
    }
}
