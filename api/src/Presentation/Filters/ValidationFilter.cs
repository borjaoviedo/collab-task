
namespace Api.Filters
{
    /// <summary>
    /// Endpoint filter that performs FluentValidation checks on request DTOs before handler execution.
    /// Converts validation failures into RFC 7807 <c>application/problem+json</c> responses (400 ValidationProblem).
    /// </summary>
    public sealed class ValidationFilter<T> : IEndpointFilter
    {
        /// <summary>
        /// Executes the validation logic for the endpoint’s input model.
        /// Resolves the validator for <typeparamref name="T"/> from DI, validates the request argument,
        /// and short-circuits the pipeline with a 400 response if validation fails.
        /// </summary>
        /// <param name="context">The endpoint invocation context containing arguments and services.</param>
        /// <param name="next">The next delegate in the endpoint pipeline.</param>
        /// <returns>A validation result or the continuation of the pipeline.</returns>
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var validator = context.HttpContext.RequestServices.GetRequiredService<FluentValidation.IValidator<T>>();
            var arg = context.Arguments.OfType<T>().FirstOrDefault();

            if (arg is not null)
            {
                var result = await validator.ValidateAsync(arg, context.HttpContext.RequestAborted);

                if (!result.IsValid)
                    return Results.ValidationProblem(result.ToDictionary());
            }

            return await next(context);
        }
    }
}
