using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Security;
using Application.Common.Abstractions.Time;
using Application.ProjectMembers.Abstractions;
using Application.Projects.Abstractions;
using Application.Users.Abstractions;
using Infrastructure.Common.Persistence;
using Infrastructure.Common.Time;
using Infrastructure.Data;
using Infrastructure.Data.Interceptors;
using Infrastructure.Data.Repositories;
using Infrastructure.Initialization;
using Infrastructure.Projects.Queries;
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
            services
                .AddSingleton<IDateTimeProvider, SystemDateTimeProvider>()
                .AddScoped<AuditingSaveChangesInterceptor>();

            // Security
            services
                .AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>()
                .AddScoped<IJwtTokenService, JwtTokenService>();

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

            // Readers / queries (used by authorization handlers to check project membership)
            services.AddScoped<IProjectMembershipReader, ProjectMembershipReader>();

            // DB init as hosted service
            services.AddHostedService<DbInitHostedService>();

            return services;
        }
    }
}
