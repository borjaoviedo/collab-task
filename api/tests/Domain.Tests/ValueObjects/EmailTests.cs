using Domain.ValueObjects;
using FluentAssertions;
using TestHelpers.Common.Testing;

namespace Domain.Tests.ValueObjects
{
    [UnitTest]
    public class EmailTests
    {
        private readonly string _defaultEmail = "test@email.com";

        [Fact]
        public void Create_ValidEmail_ReturnsInstance()
        {
            var email = Email.Create(_defaultEmail);

            email.Value.Should().Be(_defaultEmail);
        }

        [Theory]
        [InlineData("under_score@demo.com")]
        [InlineData("dash-name@demo.com")]
        public void Create_Allows_Common_LocalChars(string input)
            => Email.Create(input).Value.Should().Be(input.ToLowerInvariant());

        [Fact]
        public void Create_MinLength_Passes()
            => Email.Create("x@y.c").Value.Should().HaveLength(5);

        [Fact]
        public void Create_MaxLength256_Passes()
            => Email.Create(new string('x', 247) + "@demo.com").Value.Should().HaveLength(256);

        [Theory]
        [InlineData(" ")]
        [InlineData("   not-trimmed.email@demo.com   ")]
        public void Create_Trim_Applied_Correctly(string input)
        {
            var trimmedInput = input.Trim();

            if (trimmedInput.Length == 0)
                Assert.Throws<ArgumentException>(() => Email.Create(input));
            else
                Email.Create(input).Value.Should().Be(trimmedInput);
        }

        [Theory]
        [InlineData("UPPER@DEMO.COM", "upper@demo.com")]
        [InlineData("  MixedCase@Demo.Com  ", "mixedcase@demo.com")]
        public void Create_Normalizes_ToLower_And_Trim(string input, string expected)
            => Email.Create(input).Value.Should().Be(expected);

        [Theory]
        [InlineData("first_email_with_out_domain")]
        [InlineData("second_email_with_out_domain@")]
        [InlineData("third_email_with_out_domain@demo")]
        [InlineData("email_with_multiple_domains@first.com@second.com")]
        [InlineData("@email_without_local_part.com")]
        public void Create_InvalidFormat_Throws(string input)
            => Assert.Throws<ArgumentException>(() => Email.Create(input));

        [Fact]
        public void Create_TooLongEmail_Throws()
        {
            var random = new Random();
            var chars = Enumerable.Range(0, 248)
                .Select(_ => (char)random.Next('a', 'z' + 1))
                .ToArray();
            var tooLongEmail = new string(chars) + "@demo.com";

            Assert.Throws<ArgumentOutOfRangeException>(() => Email.Create(tooLongEmail));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_NullEmptyOrWhitespace_Throws(string? input)
            => Assert.Throws<ArgumentException>(() => Email.Create(input!));

        [Theory]
        [InlineData("t est@demo.com")]
        [InlineData("test @demo.com")]
        [InlineData("test@ demo.com")]
        [InlineData("test@de mo.com")]
        [InlineData("test@demo .com")]
        [InlineData("test@demo. com")]
        [InlineData("test@demo.co m")]
        public void Create_EmailWithSpaces_Throws(string input)
            => Assert.Throws<ArgumentException>(() => Email.Create(input));

        [Fact]
        public void ToString_ReturnsValue()
        {
            var email = Email.Create(_defaultEmail);

            email.ToString().Should().Be(_defaultEmail);
        }

        [Fact]
        public void Equality_SameValue_True()
        {
            var emailA = Email.Create(_defaultEmail);
            var emailB = Email.Create(_defaultEmail);

            emailA.Equals(emailB).Should().BeTrue();
        }

        [Fact]
        public void Equality_DifferentValue_False()
        {
            var emailA = Email.Create(_defaultEmail);
            var emailB = Email.Create("different@email.com");

            emailA.Equals(emailB).Should().BeFalse();
        }

        [Fact]
        public void Equality_IgnoresCase()
        {
            var emailA = Email.Create("User@Domain.com");
            var emailB = Email.Create("user@domain.com");
            emailA.Should().Be(emailB);
            emailA.GetHashCode().Should().Be(emailB.GetHashCode());
        }

        [Fact]
        public void Operators_Equality_SameValue_True()
        {
            var emailA = Email.Create(_defaultEmail);
            var emailB = Email.Create(_defaultEmail);

            (emailA == emailB).Should().BeTrue();
            emailA.GetHashCode().Should().Be(emailB.GetHashCode());
        }

        [Fact]
        public void Operators_Equality_DifferentValue_False()
        {
            var emailA = Email.Create(_defaultEmail);
            var emailB = Email.Create("different@email.com");

            (emailA == emailB).Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_SameValue_False()
        {
            var emailA = Email.Create(_defaultEmail);
            var emailB = Email.Create(_defaultEmail);

            (emailA != emailB).Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_DifferentValue_True()
        {
            var emailA = Email.Create(_defaultEmail);
            var emailB = Email.Create("different@email.com");

            (emailA != emailB).Should().BeTrue();
        }

        [Fact]
        public void Operators_Handle_Nulls()
        {
            Email? emailA = null;
            Email? emailB = null;
            var emailC = Email.Create(_defaultEmail);

            (emailA == emailB).Should().BeTrue();
            (emailA == emailC).Should().BeFalse();
            (emailC != null).Should().BeTrue();
        }

        [Fact]
        public void Implicit_ToString_Works()
        {
            Email email = Email.Create(_defaultEmail);
            string str = email;

            str.Should().Be(_defaultEmail);
        }
    }
}
