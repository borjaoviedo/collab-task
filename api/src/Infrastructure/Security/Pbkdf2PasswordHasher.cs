using Application.Common.Abstractions.Security;
using System.Security.Cryptography;

namespace Infrastructure.Security
{
    public sealed class Pbkdf2PasswordHasher : IPasswordHasher
    {
        const int Iterations = 100_000;
        const int SaltSize = 16;
        const int HashSize = 32;

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

        public bool Verify(string password, byte[] salt, byte[] hash)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            var computed = pbkdf2.GetBytes(HashSize);
            return CryptographicOperations.FixedTimeEquals(computed, hash);
        }
    }
}
