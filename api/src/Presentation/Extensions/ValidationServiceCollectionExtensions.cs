using Application.Common.Validation;
using FluentValidation;

namespace Api.Extensions
{
    public static class ValidationServiceCollectionExtensions
    {
        public static IServiceCollection AddAppValidation(this IServiceCollection services)
        {
            services.AddValidatorsFromAssembly(typeof(ApplicationValidationMarker).Assembly);
            return services;
        }
    }
}
