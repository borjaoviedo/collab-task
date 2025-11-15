using Api.Concurrency;
using Application.Common.Exceptions;
using System.Reflection;

namespace Api.Filters
{
    /// <summary>
    /// Validates an <c>If-Match</c> precondition against the current resource RowVersion (Base64).
    /// Resolves the resource via <c>TReadService</c>, reads <c>TDto.RowVersion</c>,
    /// and throws on mismatch or missing preconditions. Supports a <c>$me</c> mode that calls
    /// <c>GetCurrentAsync(CancellationToken)</c> instead of <c>GetByIdAsync(Guid, CancellationToken)</c>.
    /// </summary>
    /// <typeparam name="TReadService">Service exposing <c>GetByIdAsync(Guid, CancellationToken)</c> and, 
    /// for self-mode, <c>GetCurrentAsync(CancellationToken)</c>.</typeparam>
    /// <typeparam name="TDto">DTO exposing a public <c>string RowVersion</c> property.</typeparam>
    public sealed class EnsureIfMatchFilter<TReadService, TDto> : IEndpointFilter
        where TReadService : class
        where TDto : class
    {
        private readonly string _routeKey;

        // Cached reflection: resolves once per closed generic to avoid per-request lookups.
        private static readonly MethodInfo s_getByIdAsync =
            typeof(TReadService).GetMethod(
                "GetByIdAsync",
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: [typeof(Guid), typeof(CancellationToken)],
                modifiers: null)
            ?? throw new InvalidOperationException($"{typeof(TReadService).Name} must expose GetByIdAsync(Guid, CancellationToken).");

        private static readonly PropertyInfo s_rowVersionProp =
            typeof(TDto).GetProperty("RowVersion", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"{typeof(TDto).Name} must expose string RowVersion {{ get; }}.");

        /// <summary>
        /// Creates the filter.
        /// Pass <c>"userId"</c> (o el nombre real del parámetro de ruta) para endpoints por id,
        /// o <c>"$me"</c> para endpoints del usuario actual.
        /// </summary>
        /// <param name="routeValueKey">Clave de ruta para el id o <c>$me</c> para modo self.</param>
        /// <exception cref="ArgumentException">Si <paramref name="routeValueKey"/> es nulo o vacío.</exception>
        public EnsureIfMatchFilter(string routeValueKey)
        {
            if (string.IsNullOrWhiteSpace(routeValueKey))
                throw new ArgumentException("Route value key is required.", nameof(routeValueKey));

            _routeKey = routeValueKey;
        }

        /// <summary>
        /// Resolves the identifier, loads the current DTO, extracts its Base64 RowVersion, and enforces the precondition.
        /// Uses <c>GetCurrentAsync</c> when <c>routeValueKey == "$me"</c>, otherwise <c>GetByIdAsync</c>.
        /// </summary>
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var http = context.HttpContext;

            // --- Self mode ($me): call GetCurrentAsync(CancellationToken) ---
            if (string.Equals(_routeKey, "$me", StringComparison.Ordinal))
            {
                var svc = http.RequestServices.GetRequiredService<TReadService>();

                var getCurrent = typeof(TReadService).GetMethod(
                    "GetCurrentAsync",
                    BindingFlags.Public | BindingFlags.Instance,
                    binder: null,
                    types: [typeof(CancellationToken)],
                    modifiers: null)
                    ?? throw new InvalidOperationException($"{typeof(TReadService).Name} must expose GetCurrentAsync(CancellationToken).");

                var invokedCurrent = getCurrent.Invoke(svc, [http.RequestAborted]);
                if (invokedCurrent is not Task currentTask)
                    throw new InvalidOperationException("GetCurrentAsync must return Task<TDto>.");

                await currentTask.ConfigureAwait(continueOnCapturedContext: false);

                var resultPropCurrent = currentTask.GetType().GetProperty("Result");
                var dtoObjCurrent = resultPropCurrent?.GetValue(currentTask)
                    ?? throw new UnauthorizedAccessException(); // not authenticated

                var currentB64Self = s_rowVersionProp.GetValue(dtoObjCurrent) as string;

                await ConcurrencyPreconditions.EnsureMatchesOrThrowBase64Async(
                    http,
                    _ => Task.FromResult(currentB64Self),
                    http.RequestAborted).ConfigureAwait(continueOnCapturedContext: false);

                return await next(context).ConfigureAwait(continueOnCapturedContext: false);
            }

            // --- Id mode: resolve {id} and call GetByIdAsync(Guid, CancellationToken) ---
            if (!http.Request.RouteValues.TryGetValue(_routeKey, out var raw) ||
                raw is null || !Guid.TryParse(raw.ToString(), out var id))
                throw new ArgumentException($"Route value '{_routeKey}' must be a valid GUID.");

            var svcById = http.RequestServices.GetRequiredService<TReadService>();
            var invoked = s_getByIdAsync.Invoke(svcById, [id, http.RequestAborted]);

            if (invoked is not Task task)
                throw new InvalidOperationException("GetByIdAsync must return Task<TDto>.");

            await task.ConfigureAwait(continueOnCapturedContext: false);

            var resultProp = task.GetType().GetProperty("Result");
            var dtoObj = resultProp?.GetValue(task) ?? throw new NotFoundException("Resource not found.");

            var currentB64 = s_rowVersionProp.GetValue(dtoObj) as string;

            await ConcurrencyPreconditions.EnsureMatchesOrThrowBase64Async(
                http,
                _ => Task.FromResult(currentB64),
                http.RequestAborted).ConfigureAwait(continueOnCapturedContext: false);

            return await next(context).ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}
