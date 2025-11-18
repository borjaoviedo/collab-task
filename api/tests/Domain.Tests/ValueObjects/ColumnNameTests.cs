using Domain.ValueObjects;
using FluentAssertions;
using TestHelpers.Common.Testing;

namespace Domain.Tests.ValueObjects
{
    [UnitTest]
    public sealed class ColumnNameTests
    {
        private readonly string _defaultColumnName = "column name";

        [Fact]
        public void Create_ValidColumn_ReturnsInstance()
        {
            var columnName = ColumnName.Create(_defaultColumnName);

            columnName.Value.Should().Be(_defaultColumnName);
        }

        [Fact]
        public void Create_Column_With_Numbers_ReturnsInstance()
        {
            var columnNameString = "New Column 2025";
            var columnName = ColumnName.Create(columnNameString);

            columnName.Value.Should().Be(columnNameString);
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
            => Assert.Throws<ArgumentException>(() => ColumnName.Create(input));

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

            Assert.Throws<ArgumentOutOfRangeException>(() => ColumnName.Create(new string(chars)));
        }

        [Theory]
        [InlineData("c")]
        [InlineData(" C ")]
        public void Create_TooShortColumnName_Throws(string input)
            => Assert.Throws<ArgumentOutOfRangeException>(() => ColumnName.Create(input));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_NullEmptyOrWhitespace_Throws(string? input)
            => Assert.Throws<ArgumentException>(() => ColumnName.Create(input!));

        [Fact]
        public void ToString_ReturnsValue()
        {
            var columnName = ColumnName.Create(_defaultColumnName);

            columnName.ToString().Should().Be(_defaultColumnName);
        }

        [Fact]
        public void Equality_SameValue_True()
        {
            var columnNameA = ColumnName.Create(_defaultColumnName);
            var columnNameB = ColumnName.Create(_defaultColumnName);

            columnNameA.Equals(columnNameB).Should().BeTrue();
        }

        [Fact]
        public void Equality_DifferentValue_False()
        {
            var columnNameA = ColumnName.Create(_defaultColumnName);
            var columnNameB = ColumnName.Create("different name");

            columnNameA.Equals(columnNameB).Should().BeFalse();
        }

        [Fact]
        public void Equality_IgnoresCase()
        {
            var columnNameA = ColumnName.Create("First Column");
            var columnNameB = ColumnName.Create("first column");

            columnNameA.Should().Be(columnNameB);
            columnNameA.GetHashCode().Should().Be(columnNameB.GetHashCode());
        }

        [Fact]
        public void Operators_Equality_SameValue_True()
        {
            var columnNameA = ColumnName.Create(_defaultColumnName);
            var columnNameB = ColumnName.Create(_defaultColumnName);

            (columnNameA == columnNameB).Should().BeTrue();
            columnNameA.GetHashCode().Should().Be(columnNameB.GetHashCode());
        }

        [Fact]
        public void Operators_Equality_DifferentValue_False()
        {
            var columnNameA = ColumnName.Create(_defaultColumnName);
            var columnNameB = ColumnName.Create("different name");

            (columnNameA == columnNameB).Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_SameValue_False()
        {
            var columnNameA = ColumnName.Create(_defaultColumnName);
            var columnNameB = ColumnName.Create(_defaultColumnName);

            (columnNameA != columnNameB).Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_DifferentValue_True()
        {
            var columnNameA = ColumnName.Create(_defaultColumnName);
            var columnNameB = ColumnName.Create("different name");

            (columnNameA != columnNameB).Should().BeTrue();
        }

        [Fact]
        public void Operators_Handle_Nulls()
        {
            ColumnName? columnNameA = null;
            ColumnName? columnNameB = null;
            var columnNameC = ColumnName.Create(_defaultColumnName);

            (columnNameA == columnNameB).Should().BeTrue();
            (columnNameA == columnNameC).Should().BeFalse();
            (columnNameC != null).Should().BeTrue();
        }

        [Fact]
        public void Implicit_ToString_Works()
        {
            ColumnName columnName = ColumnName.Create(_defaultColumnName);
            string str = columnName;

            str.Should().Be(_defaultColumnName);
        }
    }
}
