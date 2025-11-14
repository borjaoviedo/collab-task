using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.ModelBuilderExtensions
{
    /// <summary>
    /// Provides helper extensions to apply SQL Server–specific constraints
    /// and rules on top of the provider-agnostic EF Core model.
    /// </summary>
    internal static class SqlServerModelBuilderExtensions
    {
        /// <summary>
        /// Applies SQL Server–specific constraints for all configured entities.
        /// </summary>
        /// <param name="modelBuilder">The model builder instance to extend.</param>
        public static void ConfigureSqlServerConstraints(this ModelBuilder modelBuilder)
        {
            modelBuilder.ConfigureUserSqlServerConstraints();
            modelBuilder.ConfigureProjectMemberSqlServerConstraints();
            modelBuilder.ConfigureProjectSqlServerConstraints();
            modelBuilder.ConfigureLaneSqlServerConstraints();
            modelBuilder.ConfigureColumnSqlServerConstraints();
            modelBuilder.ConfigureTaskItemSqlServerConstraints();
            modelBuilder.ConfigureTaskNoteSqlServerConstraints();
            modelBuilder.ConfigureTaskActivitySqlServerConstraints();
            modelBuilder.ConfigureTaskAssignmentSqlServerConstraints();
        }

        /// <summary>
        /// Applies SQL Server–specific CHECK constraints for the <see cref="User"/> entity.
        /// </summary>
        /// <param name="modelBuilder">The model builder instance to extend.</param>
        private static void ConfigureUserSqlServerConstraints(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().ToTable("Users", t =>
            {
                t.HasCheckConstraint("CK_Users_Email_NotEmpty", "LEN(LTRIM(RTRIM([Email]))) > 0");
                t.HasCheckConstraint("CK_Users_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                t.HasCheckConstraint("CK_Users_PasswordHash_Length_32", "DATALENGTH([PasswordHash]) = 32");
                t.HasCheckConstraint("CK_Users_PasswordSalt_Length_16", "DATALENGTH([PasswordSalt]) = 16");
            });
        }

        /// <summary>
        /// Applies SQL Server–specific CHECK constraints for the <see cref="ProjectMember"/> entity.
        /// </summary>
        /// <param name="modelBuilder">The model builder instance to extend.</param>
        private static void ConfigureProjectMemberSqlServerConstraints(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProjectMember>().ToTable("ProjectMembers", t =>
            {
                t.HasCheckConstraint("CK_ProjectMembers_RemovedAt_After_JoinedAt", "[RemovedAt] IS NULL OR [RemovedAt] >= [JoinedAt]");
            });

            modelBuilder.Entity<ProjectMember>(e =>
            {
                // Exactly one active Owner per project
                e.HasIndex(m => m.ProjectId)
                    .IsUnique()
                    .HasFilter("[Role] = 'Owner' AND [RemovedAt] IS NULL")
                    .HasDatabaseName("UX_ProjectMembers_ProjectId_ActiveOwner");
            });
        }

        /// <summary>
        /// Applies SQL Server–specific CHECK constraints for the <see cref="Project"/> entity.
        /// </summary>
        /// <param name="modelBuilder">The model builder instance to extend.</param>
        private static void ConfigureProjectSqlServerConstraints(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Project>().ToTable("Projects", t =>
            {
                t.HasCheckConstraint("CK_Projects_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                t.HasCheckConstraint("CK_Projects_UpdatedAt_GTE_CreatedAt", "[UpdatedAt] >= [CreatedAt]");
                t.HasCheckConstraint("CK_Projects_Slug_Lowercase", "[Slug] = LOWER([Slug])");
                t.HasCheckConstraint("CK_Projects_Slug_NoSpaces", "[Slug] NOT LIKE '% %'");
                t.HasCheckConstraint("CK_Projects_Slug_NoDoubleDash", "[Slug] NOT LIKE '%--%'");
                t.HasCheckConstraint("CK_Projects_Slug_NoLeadingDash", "[Slug] NOT LIKE '-%'");
                t.HasCheckConstraint("CK_Projects_Slug_NoTrailingDash", "[Slug] NOT LIKE '%-'");
            });
        }

        /// <summary>
        /// Applies SQL Server–specific CHECK constraints for the <see cref="Lane"/> entity.
        /// </summary>
        /// <param name="modelBuilder">The model builder instance to extend.</param>
        private static void ConfigureLaneSqlServerConstraints(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Lane>().ToTable("Lanes", t =>
            {
                t.HasCheckConstraint("CK_Lanes_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                t.HasCheckConstraint("CK_Lanes_Order_NonNegative", "[Order] >= 0");
            });
        }

        /// <summary>
        /// Applies SQL Server–specific CHECK constraints for the <see cref="Column"/> entity.
        /// </summary>
        /// <param name="modelBuilder">The model builder instance to extend.</param>
        private static void ConfigureColumnSqlServerConstraints(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Column>().ToTable("Columns", t =>
            {
                t.HasCheckConstraint("CK_Columns_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                t.HasCheckConstraint("CK_Columns_Order_NonNegative", "[Order] >= 0");
            });
        }

        /// <summary>
        /// Applies SQL Server–specific CHECK constraints for the <see cref="TaskItem"/> entity.
        /// </summary>
        /// <param name="modelBuilder">The model builder instance to extend.</param>
        private static void ConfigureTaskItemSqlServerConstraints(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaskItem>().ToTable("Tasks", t =>
            {
                t.HasCheckConstraint("CK_Tasks_Title_NotEmpty", "LEN(LTRIM(RTRIM([Title]))) > 0");
                t.HasCheckConstraint("CK_Tasks_Description_NotEmpty", "LEN(LTRIM(RTRIM([Description]))) > 0");
                t.HasCheckConstraint("CK_Tasks_SortKey_NonNegative", "[SortKey] >= 0");
            });
        }

        /// <summary>
        /// Applies SQL Server–specific CHECK constraints for the <see cref="TaskNote"/> entity.
        /// </summary>
        /// <param name="modelBuilder">The model builder instance to extend.</param>
        private static void ConfigureTaskNoteSqlServerConstraints(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaskNote>().ToTable("Notes", t =>
            {
                t.HasCheckConstraint("CK_Notes_Content_NotEmpty", "LEN(LTRIM(RTRIM([Content]))) > 0");
            });
        }

        /// <summary>
        /// Applies SQL Server–specific CHECK constraints for the <see cref="TaskActivity"/> entity.
        /// </summary>
        /// <param name="modelBuilder">The model builder instance to extend.</param>
        private static void ConfigureTaskActivitySqlServerConstraints(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaskActivity>().ToTable("TaskActivities", t =>
            {
                t.HasCheckConstraint("CK_TaskActivities_Payload_NotEmpty", "LEN(LTRIM(RTRIM([Payload]))) > 0");
            });
        }

        /// <summary>
        /// Applies SQL Server–specific CHECK constraints for the <see cref="TaskAssignment"/> entity.
        /// </summary>
        /// <param name="modelBuilder">The model builder instance to extend.</param>
        private static void ConfigureTaskAssignmentSqlServerConstraints(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaskAssignment>().ToTable("Assignments", t =>
            {
                t.HasCheckConstraint("CK_Assignments_Role_NotEmpty", "LEN(LTRIM(RTRIM([Role]))) > 0");
                t.HasCheckConstraint("CK_Assignments_Role_Valid", "[Role] IN ('Owner','CoOwner')");
            });

            modelBuilder.Entity<TaskAssignment>(e =>
            {
                e.HasIndex(a => new { a.TaskId, a.Role })
                 .IsUnique()
                 .HasFilter("[Role] = 'Owner'")
                 .HasDatabaseName("UX_Assignments_Task_Owner");
            });
        }
    }
}
