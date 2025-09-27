using Application.Common.Abstractions.Time;
using Infrastructure.Common;
using Infrastructure.Data;
using Infrastructure.Data.Interceptors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
        {
            services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
            services.AddScoped<AuditingSaveChangesInterceptor>();

            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.UseSqlServer(connectionString);
                options.AddInterceptors(sp.GetRequiredService<AuditingSaveChangesInterceptor>());
            });

            return services;
        }
    }
}
