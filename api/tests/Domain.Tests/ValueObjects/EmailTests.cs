using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.Tests.ValueObjects
{
    public class EmailTests
    {

        [Fact]
        public void Create_ValidEmail_ReturnsInstance()
        {
            var e = Email.Create("testemail123@demo.com");
            e.Value.Should().Be("testemail123@demo.com");
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
            => Assert.Throws<ArgumentException>(() => Email.Create(input!));

        [Fact]
        public void Create_TooLongEmail_Throws()
        {
            var random = new Random();
            var chars = Enumerable.Range(0, 248)
                .Select(_ => (char)random.Next('a', 'z' + 1))
                .ToArray();
            var tooLongEmail = new string(chars) + "@demo.com";

            Assert.Throws<ArgumentException>(() => Email.Create(tooLongEmail));
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
            => Email.Create("new.email@demo2.com").ToString().Should().Be("new.email@demo2.com");

        [Fact]
        public void Equality_SameValue_True()
        {
            var a = Email.Create("same@email.com");
            var b = Email.Create("same@email.com");
            a.Equals(b).Should().BeTrue();
        }

        [Fact]
        public void Equality_DifferentValue_False()
        {
            var a = Email.Create("first@email.com");
            var b = Email.Create("notfirst@email.com");
            a.Equals(b).Should().BeFalse();
        }

        [Fact]
        public void Equality_IgnoresCase()
        {
            var a = Email.Create("User@Domain.com");
            var b = Email.Create("user@domain.com");
            a.Should().Be(b);
            a.GetHashCode().Should().Be(b.GetHashCode());
        }

        [Fact]
        public void Operators_Equality_SameValue_True()
        {
            var a = Email.Create("valid-email@valid.com");
            var b = Email.Create("valid-email@valid.com");

            var result = a == b;
            result.Should().BeTrue();
            a.GetHashCode().Should().Be(b.GetHashCode());
        }

        [Fact]
        public void Operators_Equality_DifferentValue_False()
        {
            var a = Email.Create("valid-email@valid.com");
            var b = Email.Create("valid-email2@valid.com");

            var result = a == b;
            result.Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_SameValue_False()
        {
            var a = Email.Create("valid-email@valid.com");
            var b = Email.Create("valid-email@valid.com");

            var result = a != b;
            result.Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_DifferentValue_True()
        {
            var a = Email.Create("valid-email@valid.com");
            var b = Email.Create("valid-email2@valid.com");

            var result = a != b;
            result.Should().BeTrue();
        }

        [Fact]
        public void Operators_Handle_Nulls()
        {
            Email? a = null;
            Email? b = null;
            (a == b).Should().BeTrue();
            var c = Email.Create("x@d.com");
            (a == c).Should().BeFalse();
            (c != null).Should().BeTrue();
        }

        [Fact]
        public void Implicit_ToString_Works()
        {
            Email e = Email.Create("something@demo.com");
            string s = e;
            s.Should().Be("something@demo.com");
        }
    }
}
