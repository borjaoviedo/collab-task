using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Tests.Containers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Database
{
    [Collection("SqlServerContainer")]
    public sealed class DatabaseMigrationTests(MsSqlContainerFixture fx)
    {
        private readonly string _cs = fx.ConnectionString;

        [Fact]
        public async Task Migrations_Applied_And_Tables_Exist()
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(_cs)
                .Options;

            await using var db = new AppDbContext(opts);

            (await db.Database.CanConnectAsync()).Should().BeTrue();

            var tableCount = await db.Database
                                    .SqlQueryRaw<int>("SELECT CAST(COUNT(*) AS int) AS [Value] FROM sys.tables")
                                    .SingleAsync();

            tableCount.Should().BeGreaterThan(0);
        }
    }
}
