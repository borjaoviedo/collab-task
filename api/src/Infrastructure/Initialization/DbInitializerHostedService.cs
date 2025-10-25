using Infrastructure.Data.Extensions;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.Initialization
{
    public sealed class DbInitHostedService(IServiceProvider sp, IHostEnvironment env) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (env.IsEnvironment("Testing")) return;
            if (string.Equals(Environment.GetEnvironmentVariable("DISABLE_DB_INIT"), "true", StringComparison.OrdinalIgnoreCase))
                return;

            await sp.ApplyMigrationsAndSeedAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
