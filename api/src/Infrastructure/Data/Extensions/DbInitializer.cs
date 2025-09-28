using Infrastructure.Data.Seeders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.Data.Extensions
{
    public static class DbInitializer
    {
        public static async Task ApplyMigrationsAndSeedAsync(
            this IServiceProvider services, CancellationToken ct = default)
        {
            using var scope = services.CreateScope();
            var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await db.Database.MigrateAsync(ct);

            if (env.IsDevelopment())
                await DevSeeder.SeedAsync(services, ct);
        }
    }
}
