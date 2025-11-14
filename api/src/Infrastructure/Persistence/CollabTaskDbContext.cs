using Domain.Entities;
using Infrastructure.Persistence.ModelBuilderExtensions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence
{
    /// <summary>
    /// EF Core database context for CollabTask.
    /// Configures entity sets and provider-specific mappings, including
    /// constraints, indexes, value converters, and concurrency tokens.
    /// </summary>
    public sealed class CollabTaskDbContext(DbContextOptions<CollabTaskDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectMember> ProjectMembers { get; set; }
        public DbSet<Lane> Lanes { get; set; }
        public DbSet<Column> Columns { get; set; }
        public DbSet<TaskItem> TaskItems { get; set; }
        public DbSet<TaskNote> TaskNotes { get; set; }
        public DbSet<TaskActivity> TaskActivities { get; set; }
        public DbSet<TaskAssignment> TaskAssignments { get; set; }

        /// <summary>
        /// Applies entity configurations and delegates provider-specific adjustments.
        /// </summary>
        /// <param name="modelBuilder">The model builder used to configure EF Core mappings.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(CollabTaskDbContext).Assembly);
            ConfigureProviderSpecificMappings(modelBuilder);
        }

        /// <summary>
        /// Applies database-provider-specific mappings and constraints.
        /// For SQL Server: table names, CHECK constraints, filtered unique indexes.
        /// For SQLite: CHECK constraints, rowversion emulation, DateTimeOffset converters.
        /// </summary>
        /// <param name="modelBuilder">The model builder used to configure EF Core mappings.</param>
        private void ConfigureProviderSpecificMappings(ModelBuilder modelBuilder)
        {
            // SQL Server
            if (Database.IsSqlServer())
            {
                modelBuilder.ConfigureSqlServerConstraints();
                return;
            }

            // SQLite (testing)
            ConfigureSqliteAdjustments(modelBuilder);
        }

        /// <summary>
        /// Applies SQLite-specific adjustments such as <see cref="DateTimeOffset"/> conversion
        /// and rowversion emulation for concurrency tokens.
        /// </summary>
        /// <param name="modelBuilder">The model builder used to configure EF Core mappings.</param>
        private static void ConfigureSqliteAdjustments(ModelBuilder modelBuilder)
        {
            modelBuilder.ConfigureDateTimeOffsetForSqlite();
            modelBuilder.ConfigureDecimalForSqlite();
            modelBuilder.ConfigureRowVersionForSqlite();
        }
    }
}
