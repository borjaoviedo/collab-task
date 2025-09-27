
namespace Application.Abstractions.Security
{
    public interface IPasswordHasher
    {
        (byte[] hash, byte[] salt) Hash(string password);
        bool Verify(string password, byte[] salt, byte[] expectedHash);
    }
}
