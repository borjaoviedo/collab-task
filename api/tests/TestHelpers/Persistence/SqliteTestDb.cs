using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;

namespace TestHelpers.Persistence;

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

    public AppDbContext CreateContext()
    {
        var context = new AppDbContext(_options);

        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        return context;
    }

    public void Dispose() => _conn.Dispose();
}
