using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.Tests.ValueObjects
{
    public sealed class TaskTitleTests
    {
        private readonly string _defaultTaskTitle = "task title";

        [Fact]
        public void Create_ValidTaskTitle_ReturnsInstance()
        {
            var taskTitle = TaskTitle.Create(_defaultTaskTitle);

            taskTitle.Value.Should().Be(_defaultTaskTitle);
        }

        [Fact]
        public void Create_TaskTitle_With_Numbers_ReturnsInstance()
        {
            var taskTitle = TaskTitle.Create("Task Title 2025");

            taskTitle.Value.Should().Be("Task Title 2025");
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
            => Assert.Throws<ArgumentException>(() => TaskTitle.Create(input));

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

            Assert.Throws<ArgumentOutOfRangeException>(() => TaskTitle.Create(new string(chars)));
        }

        [Theory]
        [InlineData("T")]
        [InlineData(" t ")]
        public void Create_TooShortTaskTitle_Throws(string input)
            => Assert.Throws<ArgumentOutOfRangeException>(() => TaskTitle.Create(input));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_NullEmptyOrWhitespace_Throws(string input)
            => Assert.Throws<ArgumentException>(() => TaskTitle.Create(input));

        [Fact]
        public void ToString_ReturnsValue()
        {
            var taskTitle = TaskTitle.Create(_defaultTaskTitle);

            taskTitle.ToString().Should().Be(_defaultTaskTitle);
        }

        [Fact]
        public void Equality_SameValue_True()
        {
            var taskTitleA = TaskTitle.Create(_defaultTaskTitle);
            var taskTitleB = TaskTitle.Create(_defaultTaskTitle);

            taskTitleA.Equals(taskTitleB).Should().BeTrue();
        }

        [Fact]
        public void Equality_DifferentValue_False()
        {
            var taskTitleA = TaskTitle.Create(_defaultTaskTitle);
            var taskTitleB = TaskTitle.Create("different task title");

            taskTitleA.Equals(taskTitleB).Should().BeFalse();
        }

        [Fact]
        public void Equality_IgnoresCase()
        {
            var taskTitleA = TaskTitle.Create("First Title");
            var taskTitleB = TaskTitle.Create("first title");

            taskTitleA.Should().Be(taskTitleB);
            taskTitleA.GetHashCode().Should().Be(taskTitleB.GetHashCode());
        }

        [Fact]
        public void Operators_Equality_SameValue_True()
        {
            var taskTitleA = TaskTitle.Create(_defaultTaskTitle);
            var taskTitleB = TaskTitle.Create(_defaultTaskTitle);

            (taskTitleA == taskTitleB).Should().BeTrue();
            taskTitleA.GetHashCode().Should().Be(taskTitleB.GetHashCode());
        }

        [Fact]
        public void Operators_Equality_DifferentValue_False()
        {
            var taskTitleA = TaskTitle.Create(_defaultTaskTitle);
            var taskTitleB = TaskTitle.Create("different task title");

            (taskTitleA == taskTitleB).Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_SameValue_False()
        {
            var taskTitleA = TaskTitle.Create(_defaultTaskTitle);
            var taskTitleB = TaskTitle.Create(_defaultTaskTitle);

            (taskTitleA != taskTitleB).Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_DifferentValue_True()
        {
            var taskTitleA = TaskTitle.Create(_defaultTaskTitle);
            var taskTitleB = TaskTitle.Create("different task title");

            (taskTitleA != taskTitleB).Should().BeTrue();
        }

        [Fact]
        public void Operators_Handle_Nulls()
        {
            TaskTitle? taskTitleA = null;
            TaskTitle? taskTitleB = null;
            var taskTitleC = TaskTitle.Create(_defaultTaskTitle);

            (taskTitleA == taskTitleB).Should().BeTrue();
            (taskTitleA == taskTitleC).Should().BeFalse();
            (taskTitleC != null).Should().BeTrue();
        }

        [Fact]
        public void Implicit_ToString_Works()
        {
            TaskTitle taskTitle = TaskTitle.Create(_defaultTaskTitle);
            string str = taskTitle;

            str.Should().Be(_defaultTaskTitle);
        }
    }
}
