using Api.Filters;

namespace Api.Extensions
{
    /// <summary>
    /// Endpoint extensions for automatic request DTO validation.
    /// Adds a validation filter that enforces <typeparamref name="T"/> model rules before executing the handler.
    /// </summary>
    public static class EndpointValidationExtensions
    {
        /// <summary>
        /// Adds a validation filter for the specified DTO type.
        /// Requests with invalid payloads result in a 400 ProblemDetails response before handler execution.
        /// </summary>
        /// <typeparam name="T">The DTO type to validate.</typeparam>
        /// <param name="builder">The route handler builder to decorate.</param>
        /// <returns>The same builder for chaining.</returns>
        public static RouteHandlerBuilder RequireValidation<T>(this RouteHandlerBuilder builder)
            => builder.AddEndpointFilter(new ValidationFilter<T>());
    }
}
