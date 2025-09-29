using Api.Filters;

namespace Api.Extensions
{
    public static class EndpointValidationExtensions
    {
        public static RouteHandlerBuilder RequireValidation<T>(this RouteHandlerBuilder builder)
            => builder.AddEndpointFilter(new ValidationFilter<T>());
    }
}
