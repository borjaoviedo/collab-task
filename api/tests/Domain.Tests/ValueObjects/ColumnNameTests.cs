using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.Tests.ValueObjects
{
    public sealed class ColumnNameTests
    {
        [Fact]
        public void Create_ValidColumn_ReturnsInstance()
        {
            var c = ColumnName.Create("new column");
            c.Value.Should().Be("new column");
        }

        [Fact]
        public void Create_Column_With_Numbers_ReturnsInstance()
        {
            var c = ColumnName.Create("New column 2025");
            c.Value.Should().Be("New column 2025");
        }

        [Theory]
        [InlineData("New_Column")]
        [InlineData("New-Column")]
        public void Create_Allows_Common_LocalChars(string input)
            => ColumnName.Create(input).Value.Should().Be(input);

        [Fact]
        public void Create_MinLength_Passes()
            => ColumnName.Create("new").Value.Should().HaveLength(3);

        [Fact]
        public void Create_MaxLength100_Passes()
            => ColumnName.Create(new string('x', 100)).Value.Should().HaveLength(100);

        [Theory]
        [InlineData("  ")]
        [InlineData("   Not trimmed   ")]
        public void Create_Trim_Applied_Correctly(string input)
        {
            var trimmedInput = input.Trim();
            if (trimmedInput.Length == 0)
                Assert.Throws<ArgumentException>(() => ColumnName.Create(input));
            else
                ColumnName.Create(input).Value.Should().Be(trimmedInput);
        }

        [Theory]
        [InlineData("column  Create")]
        [InlineData("New Invalid  Column")]
        [InlineData("NOt  valid  column")]
        public void Create_Name_With_Two_Or_More_Consecutive_Spaces_Throws(string input)
            => Assert.Throws<ArgumentException>(() => ColumnName.Create(input!));

        [Theory]
        [InlineData("cólùmn ñame", "cólùmn ñame")]
        [InlineData("naïve name", "naïve name")]
        public void Create_Preserves_Unicode(string input, string expected)
            => ColumnName.Create(input).Value.Should().Be(expected);

        [Theory]
        [InlineData("COLUMN NAME", "COLUMN NAME")]
        [InlineData("  Mixed Case  ", "Mixed Case")]
        public void Create_Normalizes_Trim_And_Maintains_Cases(string input, string expected)
            => ColumnName.Create(input).Value.Should().Be(expected);

        [Fact]
        public void Create_TooLongColumnName_Throws()
        {
            var random = new Random();
            var chars = Enumerable.Range(0, 101)
                .Select(_ => (char)random.Next('a', 'z' + 1))
                .ToArray();

            Assert.Throws<ArgumentException>(() => ColumnName.Create(new string(chars)));
        }

        [Theory]
        [InlineData("c")]
        [InlineData(" C ")]
        public void Create_TooShortColumnName_Throws(string input)
        {
            Assert.Throws<ArgumentException>(() => ColumnName.Create(input));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_NullEmptyOrWhitespace_Throws(string? input)
            => Assert.Throws<ArgumentException>(() => ColumnName.Create(input!));

        [Fact]
        public void ToString_ReturnsValue()
            => ColumnName.Create("column name").ToString().Should().Be("column name");

        [Fact]
        public void Equality_SameValue_True()
        {
            var a = ColumnName.Create("same column");
            var b = ColumnName.Create("same column");
            a.Equals(b).Should().BeTrue();
        }

        [Fact]
        public void Equality_DifferentValue_False()
        {
            var a = ColumnName.Create("first column");
            var b = ColumnName.Create("second column");
            a.Equals(b).Should().BeFalse();
        }

        [Fact]
        public void Equality_IgnoresCase()
        {
            var a = ColumnName.Create("First column");
            var b = ColumnName.Create("first column");
            a.Should().Be(b);
            a.GetHashCode().Should().Be(b.GetHashCode());
        }

        [Fact]
        public void Operators_Equality_SameValue_True()
        {
            var a = ColumnName.Create("Same column");
            var b = ColumnName.Create("Same column");

            var result = a == b;
            result.Should().BeTrue();
            a.GetHashCode().Should().Be(b.GetHashCode());
        }

        [Fact]
        public void Operators_Equality_DifferentValue_False()
        {
            var a = ColumnName.Create("Same column");
            var b = ColumnName.Create("Not same column");

            var result = a == b;
            result.Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_SameValue_False()
        {
            var a = ColumnName.Create("Same column");
            var b = ColumnName.Create("Same column");

            var result = a != b;
            result.Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_DifferentValue_True()
        {
            var a = ColumnName.Create("Same column");
            var b = ColumnName.Create("Not same");

            var result = a != b;
            result.Should().BeTrue();
        }

        [Fact]
        public void Operators_Handle_Nulls()
        {
            ColumnName? a = null;
            ColumnName? b = null;
            (a == b).Should().BeTrue();
            var c = ColumnName.Create("column");
            (a == c).Should().BeFalse();
            (c != null).Should().BeTrue();
        }

        [Fact]
        public void Implicit_ToString_Works()
        {
            ColumnName c = ColumnName.Create("column");
            string s = c;
            s.Should().Be("column");
        }
    }
}
