namespace Api.Helpers
{
    /// <summary>
    /// Helper methods for resolving the correct <c>RowVersion</c> (ETag) used in optimistic concurrency.
    /// Reads decoded values from <see cref="HttpContext.Items"/> or loads the current version if a wildcard <c>If-Match</c> is used.
    /// </summary>
    public static class ConcurrencyHelpers
    {
        private const string RowVersionItemKey = "rowVersion";
        private const string IfMatchWildcardKey = "IfMatchWildcard";

        /// <summary>
        /// Resolves the <c>RowVersion</c> byte array from the request context or by fetching the current entity state
        /// when the client sends a wildcard <c>If-Match</c> header (<c>*</c>).
        /// </summary>
        /// <param name="context">Current HTTP context containing decoded ETag data.</param>
        /// <param name="getCurrentRowVersion">Delegate to retrieve the current row version from storage.</param>
        /// <returns>The resolved row version or <c>null</c> if not available.</returns>
        public static async Task<byte[]?> ResolveRowVersionAsync(
            HttpContext context,
            Func<Task<byte[]?>> getCurrentRowVersion)
        {
            if (context.Items.TryGetValue(RowVersionItemKey, out var fromHeader)
                && fromHeader is byte[] rv)
                return rv;

            if (context.Items.TryGetValue(IfMatchWildcardKey, out var wildcard)
                && wildcard is true)
                return await getCurrentRowVersion();

            return null;
        }

        /// <summary>
        /// Overload for resolving the <c>RowVersion</c> by loading a full entity if required.
        /// Fetches the entity using a provided delegate and extracts the concurrency token via selector.
        /// </summary>
        /// <typeparam name="T">The entity type to retrieve.</typeparam>
        /// <param name="context">Current HTTP context containing decoded ETag data.</param>
        /// <param name="getEntityAsync">Delegate to fetch the entity by cancellation token.</param>
        /// <param name="selectRowVersion">Selector that extracts the row version from the entity.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The resolved row version or <c>null</c> if the entity cannot be found.</returns>
        public static Task<byte[]?> ResolveRowVersionAsync<T>(
            HttpContext context,
            Func<CancellationToken, Task<T?>> getEntityAsync,
            Func<T, byte[]> selectRowVersion,
            CancellationToken ct)
            where T : class
            => ResolveRowVersionAsync(context, async () =>
            {
                var entity = await getEntityAsync(ct);
                return entity is null ? null : selectRowVersion(entity);
            });
    }
}
