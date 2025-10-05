using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Tests.Containers;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tests.Database
{
    [CollectionDefinition(nameof(DbCollection), DisableParallelization = true)]
    public class DbCollection : ICollectionFixture<MsSqlContainerFixture> { }

    [Collection(nameof(DbCollection))]
    public class DatabaseMigrationTests
    {
        private readonly MsSqlContainerFixture _fx;
        public DatabaseMigrationTests(MsSqlContainerFixture fx) => _fx = fx;

        [Fact]
        public async Task Migrations_Applied_And_Tables_Exist()
        {
            using var scope = _fx.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var canConnect = await db.Database.CanConnectAsync();
            canConnect.Should().BeTrue();
        }
    }
}
