using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.Tests.ValueObjects
{
    public sealed class TaskTitleTests
    {
        [Fact]
        public void Create_ValidTaskTitle_ReturnsInstance()
        {
            var t = TaskTitle.Create("Task Title");
            t.Value.Should().Be("Task Title");
        }

        [Fact]
        public void Create_TaskTitle_With_Numbers_ReturnsInstance()
        {
            var t = TaskTitle.Create("Task Title 2025");
            t.Value.Should().Be("Task Title 2025");
        }

        [Theory]
        [InlineData("New_Title")]
        [InlineData("New-Title")]
        public void Create_Allows_Common_LocalChars(string input)
            => TaskTitle.Create(input).Value.Should().Be(input);

        [Fact]
        public void Create_MinLength_Passes()
            => TaskTitle.Create("new").Value.Should().HaveLength(3);

        [Fact]
        public void Create_MaxLength100_Passes()
            => TaskTitle.Create(new string('x', 100)).Value.Should().HaveLength(100);

        [Theory]
        [InlineData("  ")]
        [InlineData("   Not trimmed   ")]
        public void Create_Trim_Applied_Correctly(string input)
        {
            var trimmedInput = input.Trim();
            if (trimmedInput.Length == 0)
                Assert.Throws<ArgumentException>(() => TaskTitle.Create(input));
            else
                TaskTitle.Create(input).Value.Should().Be(trimmedInput);
        }

        [Theory]
        [InlineData("Title  Create")]
        [InlineData("New Invalid  Title")]
        [InlineData("NOt  valid  title")]
        public void Create_TaskTitle_With_Two_Or_More_Consecutive_Spaces_Throws(string input)
            => Assert.Throws<ArgumentException>(() => TaskTitle.Create(input!));

        [Theory]
        [InlineData("títlè ñame", "títlè ñame")]
        [InlineData("naüve namë", "naüve namë")]
        public void Create_Preserves_Unicode(string input, string expected)
            => TaskTitle.Create(input).Value.Should().Be(expected);

        [Theory]
        [InlineData("TASK TITLE", "TASK TITLE")]
        [InlineData("  Mixed Case  ", "Mixed Case")]
        public void Create_Normalizes_Trim_And_Maintains_Cases(string input, string expected)
            => TaskTitle.Create(input).Value.Should().Be(expected);

        [Fact]
        public void Create_TooLongTaskTitle_Throws()
        {
            var random = new Random();
            var chars = Enumerable.Range(0, 101)
                .Select(_ => (char)random.Next('a', 'z' + 1))
                .ToArray();

            Assert.Throws<ArgumentException>(() => TaskTitle.Create(new string(chars)));
        }

        [Theory]
        [InlineData("T")]
        [InlineData(" t ")]
        public void Create_TooShortTaskTitle_Throws(string input)
        {
            Assert.Throws<ArgumentException>(() => TaskTitle.Create(input));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_NullEmptyOrWhitespace_Throws(string? input)
            => Assert.Throws<ArgumentException>(() => TaskTitle.Create(input!));

        [Fact]
        public void ToString_ReturnsValue()
            => TaskTitle.Create("task title").ToString().Should().Be("task title");

        [Fact]
        public void Equality_SameValue_True()
        {
            var a = TaskTitle.Create("same title");
            var b = TaskTitle.Create("same title");
            a.Equals(b).Should().BeTrue();
        }

        [Fact]
        public void Equality_DifferentValue_False()
        {
            var a = TaskTitle.Create("first title");
            var b = TaskTitle.Create("second title");
            a.Equals(b).Should().BeFalse();
        }

        [Fact]
        public void Equality_IgnoresCase()
        {
            var a = TaskTitle.Create("First Title");
            var b = TaskTitle.Create("first title");
            a.Should().Be(b);
            a.GetHashCode().Should().Be(b.GetHashCode());
        }

        [Fact]
        public void Operators_Equality_SameValue_True()
        {
            var a = TaskTitle.Create("Same title");
            var b = TaskTitle.Create("Same title");

            var result = a == b;
            result.Should().BeTrue();
            a.GetHashCode().Should().Be(b.GetHashCode());
        }

        [Fact]
        public void Operators_Equality_DifferentValue_False()
        {
            var a = TaskTitle.Create("Same title");
            var b = TaskTitle.Create("Not same title");

            var result = a == b;
            result.Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_SameValue_False()
        {
            var a = TaskTitle.Create("Same title");
            var b = TaskTitle.Create("Same title");

            var result = a != b;
            result.Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_DifferentValue_True()
        {
            var a = TaskTitle.Create("Same title");
            var b = TaskTitle.Create("Not title");

            var result = a != b;
            result.Should().BeTrue();
        }

        [Fact]
        public void Operators_Handle_Nulls()
        {
            TaskTitle? a = null;
            TaskTitle? b = null;
            (a == b).Should().BeTrue();
            var c = TaskTitle.Create("title");
            (a == c).Should().BeFalse();
            (c != null).Should().BeTrue();
        }

        [Fact]
        public void Implicit_ToString_Works()
        {
            TaskTitle t = TaskTitle.Create("title");
            string s = t;
            s.Should().Be("title");
        }
    }
}
