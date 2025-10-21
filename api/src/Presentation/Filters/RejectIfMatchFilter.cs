using Microsoft.Extensions.Primitives;

namespace Api.Filters
{
    public sealed class RejectIfMatchFilter : IEndpointFilter
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            StringValues ifMatch = context.HttpContext.Request.Headers.IfMatch;
            if (!StringValues.IsNullOrEmpty(ifMatch))
                return Results.StatusCode(StatusCodes.Status400BadRequest); // client misuse on create
            return await next(context);
        }
    }
}
