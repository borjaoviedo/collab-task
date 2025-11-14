using Application.Abstractions.Security;
using FluentAssertions;
using Infrastructure.Security;

namespace Infrastructure.Tests.Security
{
    public class Pbkdf2PasswordHasherTests
    {
        private readonly IPasswordHasher _sut = new Pbkdf2PasswordHasher();

        [Fact]
        public void Hash_SamePassword_ProducesDifferentHashesBecauseSaltIsRandom()
        {
            var password = "Str0ng_Pass!";
            var hashedPassword1 = _sut.Hash(password);
            var hashedPassword2 = _sut.Hash(password);

            hashedPassword1.hash.Should().NotBeNullOrEmpty();
            hashedPassword1.salt.Should().NotBeNullOrEmpty();
            hashedPassword2.hash.Should().NotBeNullOrEmpty();
            hashedPassword2.salt.Should().NotBeNullOrEmpty();

            hashedPassword1.salt.Should().NotEqual(hashedPassword2.salt);
            hashedPassword1.hash.Should().NotEqual(hashedPassword2.hash);
        }

        [Fact]
        public void Verify_ReturnsTrue_ForCorrectPassword()
        {
            var password = "Str0ng_Pass!";
            var (hash, salt) = _sut.Hash(password);

            var ok = _sut.Verify(password, salt, hash);

            ok.Should().BeTrue();
        }

        [Fact]
        public void Verify_ReturnsFalse_ForWrongPassword()
        {
            var (hash, salt) = _sut.Hash("correct");
            var ok = _sut.Verify("wrong", salt, hash);

            ok.Should().BeFalse();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Hash_Rejects_NullOrEmpty(string? password)
        {
            var act = () => _sut.Hash(password!);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Hash_OutputSizes_AreReasonable()
        {
            var (hash, salt) = _sut.Hash("Abcdefg.123");

            hash.Length.Should().BeGreaterThan(16);
            salt.Length.Should().BeGreaterThan(8);
        }
    }
}
