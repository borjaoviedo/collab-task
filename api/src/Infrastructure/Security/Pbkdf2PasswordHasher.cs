using Application.Common.Abstractions.Security;
using System.Security.Cryptography;

namespace Infrastructure.Security
{
    /// <summary>
    /// Hashes and verifies passwords using PBKDF2-HMAC-SHA256 with per-user random salt
    /// and constant-time comparison to mitigate timing attacks.
    /// </summary>
    public sealed class Pbkdf2PasswordHasher : IPasswordHasher
    {
        const int Iterations = 100_000;
        const int SaltSize = 16;
        const int HashSize = 32;

        /// <summary>
        /// Generates a random salt and computes the PBKDF2 hash for the provided password.
        /// </summary>
        /// <param name="password">Plaintext password to hash.</param>
        /// <returns>Tuple containing the derived hash and the generated salt.</returns>
        public (byte[] hash, byte[] salt) Hash(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password is required", nameof(password));

            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[SaltSize];
            rng.GetBytes(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(HashSize);

            return (hash, salt);
        }

        /// <summary>
        /// Verifies a plaintext password against a stored salt and expected hash.
        /// </summary>
        /// <param name="password">Candidate plaintext password.</param>
        /// <param name="salt">Salt used during original hash derivation.</param>
        /// <param name="expectedHash">Previously stored hash.</param>
        /// <returns><c>true</c> if the computed hash matches; otherwise <c>false</c>.</returns>
        public bool Verify(string password, byte[] salt, byte[] expectedHash)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            var computed = pbkdf2.GetBytes(HashSize);

            return CryptographicOperations.FixedTimeEquals(computed, expectedHash);
        }
    }
}
