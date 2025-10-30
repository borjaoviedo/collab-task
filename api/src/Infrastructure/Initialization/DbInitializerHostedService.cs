using Infrastructure.Data.Extensions;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.Initialization
{
    /// <summary>
    /// Hosted service that ensures the database schema is up to date and seeded at application startup.
    /// Skips execution in testing environments or when the <c>DISABLE_DB_INIT</c> environment variable is set to <c>true</c>.
    /// </summary>
    public sealed class DbInitHostedService(IServiceProvider sp, IHostEnvironment env) : IHostedService
    {
        /// <summary>
        /// Applies pending EF Core migrations and runs the seeder unless disabled by environment conditions.
        /// </summary>
        /// <param name="cancellationToken">Token to signal cancellation.</param>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (env.IsEnvironment("Testing")) return;

            if (string.Equals(
                Environment.GetEnvironmentVariable("DISABLE_DB_INIT"),
                "true",
                StringComparison.OrdinalIgnoreCase))
                return;

            await sp.ApplyMigrationsAndSeedAsync(cancellationToken);
        }

        /// <summary>
        /// No-op on service shutdown.
        /// </summary>
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
