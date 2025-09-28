using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Testcontainers.MsSql;

namespace Infrastructure.Tests.Containers
{
    public sealed class MsSqlContainerFixture : IAsyncLifetime
    {
        private MsSqlContainer _container = default!;
        public IServiceProvider Services = default!;
        public string ContainerConnectionString => _container.GetConnectionString();

        public async Task InitializeAsync()
        {
            _container = new MsSqlBuilder()
                .WithPassword("Str0ng!Passw0rd")
                .Build();

            await _container.StartAsync();

            var cs = _container.GetConnectionString();

            var sc = new ServiceCollection();

            var cfg = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Default"] = cs,
                    ["ASPNETCORE_ENVIRONMENT"] = Environments.Development
                })
                .Build();

            sc.AddSingleton<IConfiguration>(cfg);
            sc.AddSingleton<IHostEnvironment>(new HostingEnvironment
            {
                EnvironmentName = Environments.Development,
                ApplicationName = "Infrastructure.Tests",
                ContentRootPath = Directory.GetCurrentDirectory()
            });

            sc.AddInfrastructure(cs);

            Services = sc.BuildServiceProvider();

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();
        }

        public async Task DisposeAsync()
        {
            if (Services is IDisposable d) d.Dispose();
            if (_container is not null) await _container.DisposeAsync();
        }
    }
}
