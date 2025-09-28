using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Security;
using Application.Common.Abstractions.Time;
using Infrastructure.Common.Persistence;
using Infrastructure.Common.Time;
using Infrastructure.Data;
using Infrastructure.Data.Interceptors;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
        {
            services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
            services.AddScoped<AuditingSaveChangesInterceptor>();
            services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();

            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.UseSqlServer(connectionString);
                options.AddInterceptors(sp.GetRequiredService<AuditingSaveChangesInterceptor>());
            });

            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

            return services;
        }
    }
}
