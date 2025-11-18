using Domain.ValueObjects;
using FluentAssertions;
using TestHelpers.Common.Testing;

namespace Domain.Tests.ValueObjects
{
    [UnitTest]
    public sealed class LaneNameTests
    {
        private readonly string _defaultLaneName = "lane name";

        [Fact]
        public void Create_ValidLane_ReturnsInstance()
        {
            var laneName = LaneName.Create(_defaultLaneName);

            laneName.Value.Should().Be(_defaultLaneName);
        }

        [Fact]
        public void Create_Lane_With_Numbers_ReturnsInstance()
        {
            var laneName = LaneName.Create("New Lane 2025");

            laneName.Value.Should().Be("New Lane 2025");
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
        [InlineData("Lane  Create")]
        [InlineData("New Invalid  Lane")]
        [InlineData("NOt  valid  lane")]
        public void Create_Name_With_Two_Or_More_Consecutive_Spaces_Throws(string input)
            => Assert.Throws<ArgumentException>(() => LaneName.Create(input));

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

            Assert.Throws<ArgumentOutOfRangeException>(() => LaneName.Create(new string(chars)));
        }

        [Theory]
        [InlineData("L")]
        [InlineData(" l ")]
        public void Create_TooShortLaneName_Throws(string input)
            => Assert.Throws<ArgumentOutOfRangeException>(() => LaneName.Create(input));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_NullEmptyOrWhitespace_Throws(string? input)
            => Assert.Throws<ArgumentException>(() => LaneName.Create(input!));

        [Fact]
        public void ToString_ReturnsValue()
        {
            var laneName = LaneName.Create(_defaultLaneName);

            laneName.ToString().Should().Be(_defaultLaneName);
        }

        [Fact]
        public void Equality_SameValue_True()
        {
            var laneNameA = LaneName.Create(_defaultLaneName);
            var laneNameB = LaneName.Create(_defaultLaneName);

            laneNameA.Equals(laneNameB).Should().BeTrue();
        }

        [Fact]
        public void Equality_DifferentValue_False()
        {
            var laneNameA = LaneName.Create(_defaultLaneName);
            var laneNameB = LaneName.Create("different lane");

            laneNameA.Equals(laneNameB).Should().BeFalse();
        }

        [Fact]
        public void Equality_IgnoresCase()
        {
            var laneNameA = LaneName.Create("First Lane");
            var laneNameB = LaneName.Create("first lane");

            laneNameA.Should().Be(laneNameB);
            laneNameA.GetHashCode().Should().Be(laneNameB.GetHashCode());
        }

        [Fact]
        public void Operators_Equality_SameValue_True()
        {
            var laneNameA = LaneName.Create(_defaultLaneName);
            var laneNameB = LaneName.Create(_defaultLaneName);

            (laneNameA == laneNameB).Should().BeTrue();
            laneNameA.GetHashCode().Should().Be(laneNameB.GetHashCode());
        }

        [Fact]
        public void Operators_Equality_DifferentValue_False()
        {
            var laneNameA = LaneName.Create(_defaultLaneName);
            var laneNameB = LaneName.Create("different laneName");

            (laneNameA == laneNameB).Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_SameValue_False()
        {
            var laneNameA = LaneName.Create(_defaultLaneName);
            var laneNameB = LaneName.Create(_defaultLaneName);

            (laneNameA != laneNameB).Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_DifferentValue_True()
        {
            var laneNameA = LaneName.Create(_defaultLaneName);
            var laneNameB = LaneName.Create("different lane");

            (laneNameA != laneNameB).Should().BeTrue();
        }

        [Fact]
        public void Operators_Handle_Nulls()
        {
            LaneName? laneNameA = null;
            LaneName? laneNameB = null;
            var laneNameC = LaneName.Create(_defaultLaneName);

            (laneNameA == laneNameB).Should().BeTrue();
            (laneNameA == laneNameC).Should().BeFalse();
            (laneNameC != null).Should().BeTrue();
        }

        [Fact]
        public void Implicit_ToString_Works()
        {
            LaneName laneName = LaneName.Create(_defaultLaneName);
            string str = laneName;

            str.Should().Be(_defaultLaneName);
        }
    }
}
