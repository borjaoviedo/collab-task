using Api.Errors;
using Infrastructure;
using Application;

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
                .AddJwtAuthAndPolicies(config)
                .AddApplication()
                .AddAppValidation()
                .AddProblemDetailsAndExceptionMapping();

            return services;
        }
    }
}
