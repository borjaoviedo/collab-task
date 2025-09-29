using Api.Filters;
using FluentValidation;

namespace Api.Extensions
{
    public static class EndpointValidationExtensions
    {
        public static RouteHandlerBuilder RequireValidation<T>(this RouteHandlerBuilder builder)
        {
            return builder.AddEndpointFilterFactory((context, next) =>
            {
                var validator = context.ApplicationServices.GetRequiredService<IValidator<T>>();
                var filter = new ValidationFilter<T>(validator);

                return invocationContext => filter.InvokeAsync(invocationContext, next);
            });
        }
    }
}
