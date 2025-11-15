namespace Api.Concurrency
{
    /// <summary>
    /// Centralized constants for <see cref="HttpContext.Items"/> keys used by concurrency components.
    /// Shared by filters and helpers to avoid hard-coded strings and typos.
    /// </summary>
    public static class ConcurrencyContextKeys
    {
        /// <summary>
        /// Base64-encoded RowVersion extracted from the normalized If-Match header.
        /// Populated by the IfMatchRowVersionFilter when a concrete ETag (not "*") is present.
        /// </summary>
        public const string RowVersionBase64 = "rowVersionBase64";

        /// <summary>
        /// Flag set to <c>true</c> when the client sent If-Match: <c>*</c>.
        /// Indicates “accept any current version” and bypasses strict equality checks.
        /// </summary>
        public const string IfMatchWildcard = "IfMatchWildcard";
    }
}
