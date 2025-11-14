using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;

namespace TestHelpers.Persistence;

public sealed class SqliteTestDb : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly DbContextOptions<CollabTaskDbContext> _options;

    public SqliteTestDb()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        _options = new DbContextOptionsBuilder<CollabTaskDbContext>()
            .UseSqlite(_conn)
            .EnableSensitiveDataLogging()
            .Options;
    }

    public CollabTaskDbContext CreateContext()
    {
        var context = new CollabTaskDbContext(_options);

        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        return context;
    }

    public void Dispose() => _conn.Dispose();
}
