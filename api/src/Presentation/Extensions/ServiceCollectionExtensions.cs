using Api.Auth.Services;
using Api.Errors;
using Application.Common.Abstractions.Auth;
using Infrastructure;

namespace Api.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApiLayer(
            this IServiceCollection services,
            IConfiguration config,
            string connectionString)
        {
            services
                .AddCorsPolicies(config)
                .AddInfrastructure(connectionString)
                .AddSwaggerWithJwt()
                .AddJwtAuth(config)
                .AddProjectAuthorization()
                .AddHttpContextAccessor()
                .AddScoped<ICurrentUserService, CurrentUserService>()
                .AddAppValidation()
                .AddProblemDetailsAndExceptionMapping();

            return services;
        }
    }
}
