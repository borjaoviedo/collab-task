
namespace Api.Filters
{
    public sealed class ValidationFilter<T> : IEndpointFilter
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
        {
            var validator = ctx.HttpContext.RequestServices.GetRequiredService<FluentValidation.IValidator<T>>();
            var arg = ctx.Arguments.OfType<T>().FirstOrDefault();

            if (arg is not null)
            {
                var result = await validator.ValidateAsync(arg, ctx.HttpContext.RequestAborted);
                if (!result.IsValid)
                    return Results.ValidationProblem(result.ToDictionary());
            }

            return await next(ctx);
        }
    }
}
