using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.Tests.ValueObjects
{
    public sealed class TaskDescriptionTests
    {
        [Fact]
        public void Create_ValidTaskDescription_ReturnsInstance()
        {
            var t = TaskDescription.Create("This is a test description");
            t.Value.Should().Be("This is a test description");
        }

        [Fact]
        public void Create_TaskDescription_With_Numbers_ReturnsInstance()
        {
            var t = TaskDescription.Create("Task Description 2025");
            t.Value.Should().Be("Task Description 2025");
        }

        [Theory]
        [InlineData("New_Description")]
        [InlineData("New-Description.")]
        public void Create_Allows_Common_LocalChars(string input)
            => TaskDescription.Create(input).Value.Should().Be(input);

        [Fact]
        public void Create_MinLength_Passes()
            => TaskDescription.Create("new").Value.Should().HaveLength(3);

        [Fact]
        public void Create_MaxLength2000_Passes()
            => TaskDescription.Create(new string('x', 2000)).Value.Should().HaveLength(2000);

        [Theory]
        [InlineData("  ")]
        [InlineData("   Not trimmed   ")]
        public void Create_Trim_Applied_Correctly(string input)
        {
            var trimmedInput = input.Trim();
            if (trimmedInput.Length == 0)
                Assert.Throws<ArgumentException>(() => TaskDescription.Create(input));
            else
                TaskDescription.Create(input).Value.Should().Be(trimmedInput);
        }

        [Theory]
        [InlineData("Description  Create")]
        [InlineData("New  Valid  Description")]
        [InlineData("Valid      description")]
        public void Create_TaskDescription_With_Two_Or_More_Consecutive_Spaces_Passes(string input)
            => TaskDescription.Create(input).Value.Should().Be(input);

        [Theory]
        [InlineData("déscrïpçìó tîtlê", "déscrïpçìó tîtlê")]
        [InlineData("naüve déscrïpçìó", "naüve déscrïpçìó")]
        public void Create_Preserves_Unicode(string input, string expected)
            => TaskDescription.Create(input).Value.Should().Be(expected);

        [Theory]
        [InlineData("TASK DESCRIPTION", "TASK DESCRIPTION")]
        [InlineData("  Mixed Case  ", "Mixed Case")]
        public void Create_Normalizes_Trim_And_Maintains_Cases(string input, string expected)
            => TaskDescription.Create(input).Value.Should().Be(expected);

        [Fact]
        public void Create_TooLongTaskDescription_Throws()
        {
            var random = new Random();
            var chars = Enumerable.Range(0, 2001)
                .Select(_ => (char)random.Next('a', 'z' + 1))
                .ToArray();

            Assert.Throws<ArgumentOutOfRangeException>(() => TaskDescription.Create(new string(chars)));
        }

        [Theory]
        [InlineData("T")]
        [InlineData(" t ")]
        public void Create_TooShortTaskDescription_Throws(string input)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TaskDescription.Create(input));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_NullEmptyOrWhitespace_Throws(string? input)
            => Assert.Throws<ArgumentException>(() => TaskDescription.Create(input!));

        [Fact]
        public void ToString_ReturnsValue()
            => TaskDescription.Create("task desc").ToString().Should().Be("task desc");

        [Fact]
        public void Equality_SameValue_True()
        {
            var a = TaskDescription.Create("same desc");
            var b = TaskDescription.Create("same desc");
            a.Equals(b).Should().BeTrue();
        }

        [Fact]
        public void Equality_DifferentValue_False()
        {
            var a = TaskDescription.Create("first desc");
            var b = TaskDescription.Create("second desc");
            a.Equals(b).Should().BeFalse();
        }

        [Fact]
        public void Equality_IgnoresCase()
        {
            var a = TaskDescription.Create("First desc");
            var b = TaskDescription.Create("first desc");
            a.Should().Be(b);
            a.GetHashCode().Should().Be(b.GetHashCode());
        }

        [Fact]
        public void Operators_Equality_SameValue_True()
        {
            var a = TaskDescription.Create("Same desc");
            var b = TaskDescription.Create("Same desc");

            var result = a == b;
            result.Should().BeTrue();
            a.GetHashCode().Should().Be(b.GetHashCode());
        }

        [Fact]
        public void Operators_Equality_DifferentValue_False()
        {
            var a = TaskDescription.Create("Same desc");
            var b = TaskDescription.Create("Not same desc");

            var result = a == b;
            result.Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_SameValue_False()
        {
            var a = TaskDescription.Create("Same desc");
            var b = TaskDescription.Create("Same desc");

            var result = a != b;
            result.Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_DifferentValue_True()
        {
            var a = TaskDescription.Create("Same desc");
            var b = TaskDescription.Create("Not desc");

            var result = a != b;
            result.Should().BeTrue();
        }

        [Fact]
        public void Operators_Handle_Nulls()
        {
            TaskDescription? a = null;
            TaskDescription? b = null;
            (a == b).Should().BeTrue();
            var c = TaskDescription.Create("desc");
            (a == c).Should().BeFalse();
            (c != null).Should().BeTrue();
        }

        [Fact]
        public void Implicit_ToString_Works()
        {
            TaskDescription d = TaskDescription.Create("desc");
            string s = d;
            s.Should().Be("desc");
        }
    }
}
