using Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Respawn;

namespace Infrastructure.Tests.Containers
{
    public sealed class MsSqlContainerFixture : IAsyncLifetime
    {
        private MsSqlContainer _container = default!;
        private string _dbName = default!;
        private Respawner _respawner = default!;

        public string ConnectionString { get; private set; } = default!;

        public async Task InitializeAsync()
        {
            _container = new MsSqlBuilder().WithPassword("Str0ng!Passw0rd").Build();
            await _container.StartAsync();

            _dbName = $"ct_shared_{Guid.NewGuid():N}";
            var serverCs = _container.GetConnectionString();

            await using (var conn = new SqlConnection(serverCs))
            {
                await conn.OpenAsync();
                await using var cmd = new SqlCommand($"CREATE DATABASE [{_dbName}];", conn);
                await cmd.ExecuteNonQueryAsync();
            }

            ConnectionString = $"{serverCs};Database={_dbName}";

            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(ConnectionString, o => o.EnableRetryOnFailure())
                .Options;

            await using (var db = new AppDbContext(opts))
                await db.Database.MigrateAsync();

            // Configure Respawn v6 for SQL Server
            await using var resetConn = new SqlConnection(ConnectionString);
            await resetConn.OpenAsync();
            _respawner = await Respawner.CreateAsync(resetConn, new RespawnerOptions
            {
                DbAdapter = DbAdapter.SqlServer,
                SchemasToInclude = ["dbo"],
                TablesToIgnore = [new("__EFMigrationsHistory")]
            });
        }

        public async Task ResetAsync()
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();
            await _respawner.ResetAsync(conn);
        }

        public async Task DisposeAsync() => await _container.DisposeAsync();
    }
}
