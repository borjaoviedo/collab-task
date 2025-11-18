using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Tests.Containers;
using Microsoft.EntityFrameworkCore;
using TestHelpers.Common.Testing;

namespace Infrastructure.Tests.Database
{
    [IntegrationTest]
    [SqlServerContainerTest]
    [Collection("SqlServerContainer")]
    public sealed class DatabaseMigrationTests(MsSqlContainerFixture fx)
    {
        private readonly string _cs = fx.ConnectionString;

        [Fact]
        public async Task Migrations_Applied_And_Tables_Exist()
        {
            var opts = new DbContextOptionsBuilder<CollabTaskDbContext>()
                .UseSqlServer(_cs)
                .Options;

            await using var db = new CollabTaskDbContext(opts);
            var databaseCanConnect = await db.Database.CanConnectAsync();

            databaseCanConnect.Should().BeTrue();

            var tableCount = await db.Database
                                    .SqlQueryRaw<int>("SELECT CAST(COUNT(*) AS int) AS [Value] FROM sys.tables")
                                    .SingleAsync();

            tableCount.Should().BeGreaterThan(0);
        }
    }
}
