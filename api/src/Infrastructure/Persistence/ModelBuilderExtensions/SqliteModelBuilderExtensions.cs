using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Persistence.ModelBuilderExtensions
{
    /// <summary>
    /// Provides helper extensions to adjust EF Core model mappings for SQLite.
    /// Used to emulate SQL Server behaviors such as <c>rowversion</c> and <c>DateTimeOffset</c> handling,
    /// and to normalize numeric mappings like <c>decimal</c>.
    /// </summary>
    internal static class SqliteModelBuilderExtensions
    {
        /// <summary>
        /// Configures <c>RowVersion</c> emulation for all entities that define a <c>byte[] RowVersion</c> property.
        /// Uses <c>randomblob(8)</c> as default value to mimic SQL Server's <c>rowversion</c> behavior.
        /// </summary>
        /// <param name="modelBuilder">The EF Core model builder instance.</param>
        public static void ConfigureRowVersionForSqlite(this ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var property = entityType.FindProperty("RowVersion");
                if (property is null || property.ClrType != typeof(byte[]))
                {
                    continue;
                }

                property.IsConcurrencyToken = true;
                property.SetDefaultValueSql("randomblob(8)");
                property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
            }
        }

        /// <summary>
        /// Configures <see cref="DateTimeOffset"/> properties across all entities to use
        /// a <see cref="ValueConverter"/> that stores values as Unix time in milliseconds.
        /// This ensures compatibility with SQLite, which lacks native <see cref="DateTimeOffset"/> support.
        /// </summary>
        /// <param name="modelBuilder">The EF Core model builder instance.</param>
        public static void ConfigureDateTimeOffsetForSqlite(this ModelBuilder modelBuilder)
        {
            var dtoToLong = new ValueConverter<DateTimeOffset, long>(
                v => v.ToUnixTimeMilliseconds(),
                v => DateTimeOffset.FromUnixTimeMilliseconds(v));

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties()
                             .Where(p => p.ClrType == typeof(DateTimeOffset) || p.ClrType == typeof(DateTimeOffset?)))
                {
                    property.SetValueConverter(dtoToLong);
                }
            }
        }

        /// <summary>
        /// Configures <see cref="decimal"/> properties across all entities to use a
        /// <see cref="ValueConverter"/> with <see cref="double"/> as the provider type
        /// and maps them to the SQLite <c>REAL</c> column type.
        /// This normalizes decimal handling for SQLite, which does not support a native decimal type.
        /// </summary>
        /// <param name="modelBuilder">The EF Core model builder instance.</param>
        public static void ConfigureDecimalForSqlite(this ModelBuilder modelBuilder)
        {
            var decimalToDouble = new ValueConverter<decimal, double>(
                v => (double)v,
                v => (decimal)v);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties()
                             .Where(p => p.ClrType == typeof(decimal)))
                {
                    property.SetValueConverter(decimalToDouble);
                    property.SetColumnType("REAL");
                }
            }
        }
    }
}
