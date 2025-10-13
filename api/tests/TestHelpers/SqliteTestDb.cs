using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;

namespace TestHelpers;

public sealed class SqliteTestDb : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly DbContextOptions<AppDbContext> _options;

    public SqliteTestDb()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_conn)
            .EnableSensitiveDataLogging()
            .Options;
    }

    public AppDbContext CreateContext(bool recreate = true)
    {
        var ctx = new AppDbContext(_options);

        if (recreate)
        {
            ctx.Database.EnsureDeleted();
            ctx.Database.EnsureCreated();
        }

        return ctx;
    }

    public void Dispose() => _conn.Dispose();
}
