using FluentValidation;

namespace Api.Filters
{
    public sealed class ValidationFilter<T> : IEndpointFilter
    {
        private readonly IValidator<T> _validator;
        public ValidationFilter(IValidator<T> validator) => _validator = validator;

        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
        {
            var model = ctx.Arguments.OfType<T>().FirstOrDefault();
            if (model is null) return Results.BadRequest("Invalid payload.");

            var vr = await _validator.ValidateAsync(model, ctx.HttpContext.RequestAborted);
            if (!vr.IsValid)
            {
                var errors = vr.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                return Results.ValidationProblem(errors);
            }
            return await next(ctx);
        }
    }
}
