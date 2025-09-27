using Application.Common.Abstractions.Security;
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
            var p = "Str0ng_Pass!";
            var a = _sut.Hash(p);
            var b = _sut.Hash(p);

            a.hash.Should().NotBeNullOrEmpty();
            a.salt.Should().NotBeNullOrEmpty();
            b.hash.Should().NotBeNullOrEmpty();
            b.salt.Should().NotBeNullOrEmpty();

            a.salt.Should().NotEqual(b.salt);
            a.hash.Should().NotEqual(b.hash);
        }

        [Fact]
        public void Verify_ReturnsTrue_ForCorrectPassword()
        {
            var p = "Str0ng_Pass!";
            var (hash, salt) = _sut.Hash(p);

            var ok = _sut.Verify(p, hash, salt);

            ok.Should().BeTrue();
        }

        [Fact]
        public void Verify_ReturnsFalse_ForWrongPassword()
        {
            var (hash, salt) = _sut.Hash("correct");

            var ok = _sut.Verify("wrong", hash, salt);

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
