using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.Tests.ValueObjects
{
    public sealed class LaneNameTests
    {
        [Fact]
        public void Create_ValidLane_ReturnsInstance()
        {
            var l = LaneName.Create("New Lane");
            l.Value.Should().Be("New Lane");
        }

        [Fact]
        public void Create_Lane_With_Numbers_ReturnsInstance()
        {
            var l = LaneName.Create("New Lane 2025");
            l.Value.Should().Be("New Lane 2025");
        }

        [Theory]
        [InlineData("New_Lane")]
        [InlineData("New-Lane")]
        public void Create_Allows_Common_LocalChars(string input)
            => LaneName.Create(input).Value.Should().Be(input);

        [Fact]
        public void Create_MinLength_Passes()
            => LaneName.Create("new").Value.Should().HaveLength(3);

        [Fact]
        public void Create_MaxLength100_Passes()
            => LaneName.Create(new string('x', 100)).Value.Should().HaveLength(100);

        [Theory]
        [InlineData("  ")]
        [InlineData("   Not trimmed   ")]
        public void Create_Trim_Applied_Correctly(string input)
        {
            var trimmedInput = input.Trim();
            if (trimmedInput.Length == 0)
                Assert.Throws<ArgumentException>(() => LaneName.Create(input));
            else
                LaneName.Create(input).Value.Should().Be(trimmedInput);
        }

        [Theory]
        [InlineData("Line  Create")]
        [InlineData("New Invalid  Line")]
        [InlineData("NOt  valid  line")]
        public void Create_Name_With_Two_Or_More_Consecutive_Spaces_Throws(string input)
            => Assert.Throws<ArgumentException>(() => LaneName.Create(input!));

        [Theory]
        [InlineData("lánè ñame", "lánè ñame")]
        [InlineData("naïve name", "naïve name")]
        public void Create_Preserves_Unicode(string input, string expected)
            => LaneName.Create(input).Value.Should().Be(expected);

        [Theory]
        [InlineData("LANE NAME", "LANE NAME")]
        [InlineData("  Mixed Case  ", "Mixed Case")]
        public void Create_Normalizes_Trim_And_Maintains_Cases(string input, string expected)
            => LaneName.Create(input).Value.Should().Be(expected);

        [Fact]
        public void Create_TooLongLaneName_Throws()
        {
            var random = new Random();
            var chars = Enumerable.Range(0, 101)
                .Select(_ => (char)random.Next('a', 'z' + 1))
                .ToArray();

            Assert.Throws<ArgumentException>(() => LaneName.Create(new string(chars)));
        }

        [Theory]
        [InlineData("L")]
        [InlineData(" l ")]
        public void Create_TooShortLaneName_Throws(string input)
        {
            Assert.Throws<ArgumentException>(() => LaneName.Create(input));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_NullEmptyOrWhitespace_Throws(string? input)
            => Assert.Throws<ArgumentException>(() => LaneName.Create(input!));

        [Fact]
        public void ToString_ReturnsValue()
            => LaneName.Create("lane name").ToString().Should().Be("lane name");

        [Fact]
        public void Equality_SameValue_True()
        {
            var a = LaneName.Create("same lane");
            var b = LaneName.Create("same lane");
            a.Equals(b).Should().BeTrue();
        }

        [Fact]
        public void Equality_DifferentValue_False()
        {
            var a = LaneName.Create("first line");
            var b = LaneName.Create("second line");
            a.Equals(b).Should().BeFalse();
        }

        [Fact]
        public void Equality_IgnoresCase()
        {
            var a = LaneName.Create("First Line");
            var b = LaneName.Create("first line");
            a.Should().Be(b);
            a.GetHashCode().Should().Be(b.GetHashCode());
        }

        [Fact]
        public void Operators_Equality_SameValue_True()
        {
            var a = LaneName.Create("Same line");
            var b = LaneName.Create("Same line");

            var result = a == b;
            result.Should().BeTrue();
            a.GetHashCode().Should().Be(b.GetHashCode());
        }

        [Fact]
        public void Operators_Equality_DifferentValue_False()
        {
            var a = LaneName.Create("Same line");
            var b = LaneName.Create("Not same line");

            var result = a == b;
            result.Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_SameValue_False()
        {
            var a = LaneName.Create("Same line");
            var b = LaneName.Create("Same line");

            var result = a != b;
            result.Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_DifferentValue_True()
        {
            var a = LaneName.Create("Same line");
            var b = LaneName.Create("Not same");

            var result = a != b;
            result.Should().BeTrue();
        }

        [Fact]
        public void Operators_Handle_Nulls()
        {
            LaneName? a = null;
            LaneName? b = null;
            (a == b).Should().BeTrue();
            var c = LaneName.Create("line");
            (a == c).Should().BeFalse();
            (c != null).Should().BeTrue();
        }

        [Fact]
        public void Implicit_ToString_Works()
        {
            LaneName l = LaneName.Create("line");
            string s = l;
            s.Should().Be("line");
        }
    }
}
