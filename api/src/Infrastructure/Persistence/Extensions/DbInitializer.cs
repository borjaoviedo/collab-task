using Infrastructure.Persistence.Seeders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.Persistence.Extensions
{
    /// <summary>
    /// Provides database initialization helpers for applying migrations
    /// and seeding initial data during application startup.
    /// </summary>
    public static class DbInitializer
    {
        /// <summary>
        /// Applies all pending EF Core migrations and seeds development data when applicable.
        /// Executes within a scoped service provider to ensure proper dependency resolution.
        /// </summary>
        /// <param name="services">The root service provider.</param>
        /// <param name="ct">Cancellation token.</param>
        public static async Task ApplyMigrationsAndSeedAsync(
            this IServiceProvider services,
            CancellationToken ct = default)
        {
            using var scope = services.CreateScope();
            var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
            var db = scope.ServiceProvider.GetRequiredService<CollabTaskDbContext>();

            await db.Database.MigrateAsync(ct);

            if (env.IsDevelopment())
                await DevSeeder.SeedAsync(scope.ServiceProvider, ct);
        }
    }
}
