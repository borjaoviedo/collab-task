using Infrastructure.Data.Extensions;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.Initialization
{
    public sealed class DbInitHostedService(IServiceProvider sp, IHostEnvironment env) : IHostedService
    {
        public async Task StartAsync(CancellationToken ct)
        {
            if (env.IsEnvironment("Testing")) return;
            if (string.Equals(Environment.GetEnvironmentVariable("DISABLE_DB_INIT"), "true", StringComparison.OrdinalIgnoreCase))
                return;

            await sp.ApplyMigrationsAndSeedAsync(ct);
        }

        public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
