using Application.Common.Validation;
using FluentValidation;

namespace Api.Extensions
{
    /// <summary>
    /// Service collection extensions for registering FluentValidation validators used in the application layer.
    /// Scans the assembly marked by <see cref="ApplicationValidationMarker"/> and adds all validators to DI.
    /// </summary>
    public static class ValidationServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all FluentValidation validators from the application layer assembly.
        /// Enables automatic validation for DTOs and commands resolved in the API.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <returns>The same service collection for chaining.</returns>
        public static IServiceCollection AddAppValidation(this IServiceCollection services)
        {
            services.AddValidatorsFromAssembly(typeof(ApplicationValidationMarker).Assembly);
            return services;
        }
    }
}
