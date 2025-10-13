namespace Api.Helpers
{
    public static class ConcurrencyHelpers
    {
        public static async Task<byte[]> ResolveRowVersionAsync<T>( HttpContext http, Func<Task<T?>> loader, Func<T, byte[]> selector) where T : class
        {
            if (http.Items.TryGetValue("rowVersion", out var o) && o is byte[] rv && rv.Length > 0)
                return rv;

            var entity = await loader();
            return entity is null ? throw new KeyNotFoundException("Entity not found.") : selector(entity);
        }
    }
}
