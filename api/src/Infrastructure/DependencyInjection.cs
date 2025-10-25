using Application.Columns.Abstractions;
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
using Infrastructure.Data.Interceptors;
using Infrastructure.Data.Repositories;
using Infrastructure.Initialization;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    public static class DependencyInjection
    {
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
