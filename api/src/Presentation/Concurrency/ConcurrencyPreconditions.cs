using Api.ErrorHandling;
using Application.Common.Exceptions;

namespace Api.Concurrency
{
    /// <summary>
    /// Helper methods to enforce optimistic concurrency using <c>If-Match</c> and RowVersion.
    /// Validates client ETags before executing state-changing handlers.
    /// </summary>
    /// <remarks>
    /// Expects <see cref="ConcurrencyContextKeys.RowVersionBase64"/> or
    /// <see cref="ConcurrencyContextKeys.IfMatchWildcard"/> to be set by the header filter.
    /// </remarks>
    public static class ConcurrencyPreconditions
    {
        /// <summary>
        /// Ensures the decoded <c>If-Match</c> token (Base64) matches the current persisted RowVersion.
        /// Respects the wildcard (<c>*</c>) semantics.
        /// </summary>
        /// <param name="http">Current <see cref="HttpContext"/> with parsed concurrency data.</param>
        /// <param name="loadCurrentRowVersion">
        /// Delegate that retrieves the current RowVersion in Base64 format, or <c>null</c> if not found.
        /// </param>
        /// <param name="ct">Cancellation token.</param>
        /// <exception cref="OptimisticConcurrencyException">
        /// Thrown when the client ETag does not match the current resource version.
        /// </exception>
        /// <exception cref="PreconditionRequiredException">
        /// Thrown when <c>If-Match</c> is required but missing.
        /// </exception>
        public static async Task EnsureMatchesOrThrowBase64Async(
            HttpContext http,
            Func<CancellationToken, Task<string?>> loadCurrentRowVersion,
            CancellationToken ct)
        {
            // Defensive guards for caller misuse
            ArgumentNullException.ThrowIfNull(http);
            ArgumentNullException.ThrowIfNull(loadCurrentRowVersion);

            // Wildcard (*) -> client accepts any version; skip equality check
            if (http.Items.TryGetValue(ConcurrencyContextKeys.IfMatchWildcard, out var wild) && wild is true)
                return;

            // Compare Base64 tokens when the header carried a concrete ETag
            if (http.Items.TryGetValue(ConcurrencyContextKeys.RowVersionBase64, out var rv) && rv is string clientToken)
            {
                var currentToken = await loadCurrentRowVersion(ct).ConfigureAwait(continueOnCapturedContext: false);

                // Empty/missing currentToken means the resource is gone or lacks a concurrency token
                if (string.IsNullOrWhiteSpace(currentToken) ||
                    !string.Equals(clientToken, currentToken, StringComparison.Ordinal))
                {
                    throw new OptimisticConcurrencyException(
                        "Precondition failed: ETag does not match current resource version.");
                }
                return;
            }

            // No ETag and no wildcard -> header required but not supplied
            throw new PreconditionRequiredException("If-Match header is required.");
        }
    }
}
