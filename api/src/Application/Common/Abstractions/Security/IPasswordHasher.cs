namespace Application.Common.Abstractions.Security
{
    /// <summary>
    /// Hashes passwords using a strong, salted, one-way key derivation function and verifies candidates.
    /// </summary>
    public interface IPasswordHasher
    {
        /// <summary>
        /// Computes a salted hash for the provided password.
        /// </summary>
        /// <param name="password">The UTF-8 password to hash.</param>
        /// <returns>
        /// The derived hash bytes and the salt bytes that must be stored alongside the hash.
        /// </returns>
        (byte[] hash, byte[] salt) Hash(string password);

        /// <summary>
        /// Verifies a password against the expected hash using the provided salt.
        /// </summary>
        /// <param name="password">The candidate password to verify.</param>
        /// <param name="salt">The salt originally used to derive the stored hash.</param>
        /// <param name="expectedHash">The stored hash to compare against.</param>
        /// <returns><c>true</c> if the password matches; otherwise <c>false</c>.</returns>
        bool Verify(string password, byte[] salt, byte[] expectedHash);
    }
}
