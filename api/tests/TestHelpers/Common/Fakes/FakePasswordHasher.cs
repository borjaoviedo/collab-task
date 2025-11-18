
using Application.Abstractions.Security;
using System.Text;

namespace TestHelpers.Common.Fakes
{
    /// <summary>
    /// Deterministic fake password hasher for testing purposes.
    /// Not secure. Mimics hashing and verification for predictable results.
    /// </summary>
    public sealed class FakePasswordHasher : IPasswordHasher
    {
        public (byte[] hash, byte[] salt) Hash(string password)
        {
            var pwdBytes = Encoding.UTF8.GetBytes(password);

            var hash = new byte[32];
            var salt = new byte[16];

            Array.Copy(pwdBytes, hash, Math.Min(hash.Length, pwdBytes.Length));

            var saltSrc = pwdBytes.Reverse().ToArray();
            Array.Copy(saltSrc, salt, Math.Min(salt.Length, saltSrc.Length));

            return (hash, salt);
        }

        public bool Verify(string password, byte[] salt, byte[] expectedHash)
        {
            var (hash, expectedSalt) = Hash(password);
            return hash.SequenceEqual(expectedHash) && salt.SequenceEqual(expectedSalt);
        }
    }
}
