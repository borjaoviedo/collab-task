using Application.Columns.Abstractions;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Security;
using Application.Common.Abstractions.Time;
using Application.Lanes.Abstractions;
using Application.ProjectMembers.Abstractions;
using Application.Projects.Abstractions;
using Application.TaskActivities.Abstractions;
using Application.TaskAssignments.Abstractions;
using Application.TaskItems.Abstractions;
using Application.TaskNotes.Abstractions;
using Application.Users.Abstractions;
using Infrastructure.Common.Time;
using Infrastructure.Data;
using Infrastructure.Data.Initialization;
using Infrastructure.Data.Interceptors;
using Infrastructure.Data.Repositories;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    /// <summary>
    /// Infrastructure layer dependency registration.
    /// </summary>
    /// <remarks>
    /// Registers database context, repositories, security services, and infrastructure-level utilities.
    /// This method should be called from the API composition root before building the app.
    /// It encapsulates all concrete dependencies of the <c>Infrastructure</c> layer and
    /// isolates them from upper layers.
    /// </remarks>
    public static class DependencyInjection
    {
        /// <summary>
        /// Adds the Infrastructure layer services and EF Core configuration to the DI container.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
        /// <param name="connectionString">The database connection string.</param>
        /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
        {
            // Common services
            services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
            services.AddScoped<AuditingSaveChangesInterceptor>();

            // Security
            services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
            services.AddScoped<IJwtTokenService, JwtTokenService>();

            // EF Core DbContext + interceptors
            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.UseSqlServer(connectionString);
                options.AddInterceptors(sp.GetRequiredService<AuditingSaveChangesInterceptor>());
            });

            // UoW
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IProjectRepository, ProjectRepository>();
            services.AddScoped<IProjectMemberRepository, ProjectMemberRepository>();
            services.AddScoped<ILaneRepository, LaneRepository>();
            services.AddScoped<IColumnRepository, ColumnRepository>();
            services.AddScoped<ITaskItemRepository, TaskItemRepository>();
            services.AddScoped<ITaskNoteRepository, TaskNoteRepository>();
            services.AddScoped<ITaskAssignmentRepository, TaskAssignmentRepository>();
            services.AddScoped<ITaskActivityRepository, TaskActivityRepository>();

            // DB init as hosted service
            services.AddHostedService<DbInitHostedService>();

            return services;
        }
    }
}
