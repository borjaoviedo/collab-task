using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.Tests.ValueObjects
{
    public class UserNameTests
    {

        [Fact]
        public void Create_ValidName_ReturnsInstance()
        {
            var n = UserName.Create("A Name");
            n.Value.Should().Be("A Name");
        }

        [Fact]
        public void Create_Allows_Single_Spaces_Between_Words()
            => UserName.Create("John Doe Junior").Value.Should().Be("John Doe Junior");

        [Theory]
        [InlineData("José")]
        [InlineData("Jòse")]
        public void Create_Accented_Letters_Pass_With_Unicode_Regex(string input)
            => UserName.Create(input).Value.Should().Be(input);

        [Fact]
        public void Create_MinLength_Passes()
            => UserName.Create("aa").Value.Should().HaveLength(2);

        [Fact]
        public void Create_MaxLength100_Passes()
            => UserName.Create(new string('x', 100)).Value.Should().HaveLength(100);

        [Theory]
        [InlineData(" ")]
        [InlineData("   Not Trimmed   ")]
        [InlineData("   Second Not Trimmed")]
        [InlineData("Third Not Trimmed    ")]
        public void Create_Trim_Applied_Correctly(string input)
        {
            var trimmedInput = input.Trim();
            if (trimmedInput.Length == 0)
                Assert.Throws<ArgumentException>(() => UserName.Create(input));
            else
                UserName.Create(input).Value.Should().Be(trimmedInput);
        }

        [Theory]
        [InlineData("John!")]
        [InlineData("John Do3")]
        [InlineData("John Doe 1")]
        [InlineData("John_Doe")]
        [InlineData("John-Doe")]
        [InlineData("John@Doe")]
        [InlineData("John+Doe")]
        public void Create_InvalidFormat_Throws(string input)
            => Assert.Throws<ArgumentException>(() => UserName.Create(input!));

        [Theory]
        [InlineData("John  Doe")]
        [InlineData("John   Doe")]
        [InlineData("John    Doe")]
        [InlineData("John Doe  Junior")]
        [InlineData("John Doe   Junior")]
        [InlineData("John  Doe  Senior")]
        public void Create_Name_With_Two_Or_More_Consecutive_Spaces_Throws(string input)
            => Assert.Throws<ArgumentException>(() => UserName.Create(input!));

        [Fact]
        public void Create_TooLongName_Throws()
        {
            var random = new Random();
            var chars = Enumerable.Range(0, 101)
                .Select(_ => (char)random.Next('a', 'z' + 1))
                .ToArray();
            var tooLongName = new string(chars);

            Assert.Throws<ArgumentOutOfRangeException>(() => UserName.Create(tooLongName));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_NullEmptyOrWhitespace_Throws(string? input)
            => Assert.Throws<ArgumentException>(() => UserName.Create(input!));


        [Fact]
        public void ToString_ReturnsValue()
            => UserName.Create("Random User Name").ToString().Should().Be("Random User Name");

        [Fact]
        public void Equality_SameValue_True()
        {
            var a = UserName.Create("same name");
            var b = UserName.Create("same name");
            a.Equals(b).Should().BeTrue();
        }

        [Fact]
        public void Equality_DifferentValue_False()
        {
            var a = UserName.Create("same name");
            var b = UserName.Create("not same name");
            a.Equals(b).Should().BeFalse();
        }

        [Fact]
        public void Equality_IgnoresCase()
        {
            var a = UserName.Create("User Name");
            var b = UserName.Create("user name");
            a.Should().Be(b);
            a.GetHashCode().Should().Be(b.GetHashCode());
        }

        [Fact]
        public void Equality_Is_Symmetric_And_Transitive()
        {
            var a = UserName.Create("Alice Smith");
            var b = UserName.Create("alice smith");
            var c = UserName.Create("ALICE SMITH");
            a.Equals(b).Should().BeTrue();
            b.Equals(a).Should().BeTrue();
            a.Equals(b).Should().BeTrue();
            b.Equals(c).Should().BeTrue();
            a.Equals(c).Should().BeTrue();
        }

        [Fact]
        public void Operators_Equality_SameValue_True()
        {
            var a = UserName.Create("Valid Random User Name");
            var b = UserName.Create("Valid Random User Name");

            var result = a == b;
            result.Should().BeTrue();
            a.GetHashCode().Should().Be(b.GetHashCode());
        }

        [Fact]
        public void Operators_Equality_DifferentValue_False()
        {
            var a = UserName.Create("User name");
            var b = UserName.Create("Second user name");

            var result = a == b;
            result.Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_SameValue_False()
        {
            var a = UserName.Create("Common name");
            var b = UserName.Create("Common name");

            var result = a != b;
            result.Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_DifferentValue_True()
        {
            var a = UserName.Create("John");
            var b = UserName.Create("Michael");

            var result = a != b;
            result.Should().BeTrue();
        }

        [Fact]
        public void Operators_Handle_Nulls()
        {
            UserName? a = null;
            UserName? b = null;
            (a == b).Should().BeTrue();
            var c = UserName.Create("name");
            (a == c).Should().BeFalse();
            (c != null).Should().BeTrue();
        }

        [Fact]
        public void Implicit_ToString_Works()
        {
            UserName n = UserName.Create("last name");
            string s = n;
            s.Should().Be("last name");
        }
    }
}
