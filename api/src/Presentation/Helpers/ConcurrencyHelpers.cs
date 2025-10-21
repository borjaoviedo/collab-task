namespace Api.Helpers
{
    public static class ConcurrencyHelpers
    {
        private const string RowVersionItemKey = "rowVersion";
        private const string IfMatchWildcardKey = "IfMatchWildcard";

        public static async Task<byte[]?> ResolveRowVersionAsync(HttpContext context, Func<Task<byte[]?>> getCurrentRowVersion)
        {
            if (context.Items.TryGetValue(RowVersionItemKey, out var fromHeader) && fromHeader is byte[] rv)
                return rv;

            if (context.Items.TryGetValue(IfMatchWildcardKey, out var wildcard) && wildcard is true)
                return await getCurrentRowVersion();

            return null;
        }

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
