
namespace TestHelpers.Api.Common
{
    /// <summary>
    /// Provides common test utilities shared across API tests.
    /// </summary>
    public static class CommonApiTestHelpers
    {
        /// <summary>
        /// Produces a valid Base64 RowVersion that differs from the original value
        /// but preserves correct length and format for concurrency checks.
        /// </summary>
        /// <param name="currentBase64">
        /// The current RowVersion in Base64 form (must decode to the same length used by your EF model, typically 8 bytes).
        /// </param>
        /// <returns>
        /// A new Base64 string representing a mutated RowVersion that will always fail the concurrency check.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="currentBase64"/> is null, empty, or invalid Base64.
        /// </exception>
        public static string GenerateStaleRowVersion(string currentBase64)
        {
            if (string.IsNullOrWhiteSpace(currentBase64))
                throw new ArgumentException("RowVersion cannot be null or empty.", nameof(currentBase64));

            byte[] bytes;

            try
            {
                bytes = Convert.FromBase64String(currentBase64);
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("RowVersion is not valid Base64.", nameof(currentBase64), ex);
            }

            if (bytes.Length == 0)
                throw new ArgumentException("Decoded RowVersion must contain at least 1 byte.", nameof(currentBase64));

            // Mutate one byte deterministically to guarantee mismatch
            bytes[0] ^= 0xFF;

            return Convert.ToBase64String(bytes);
        }
    }
}
