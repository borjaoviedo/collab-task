using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.Tests.ValueObjects
{
    public sealed class TaskDescriptionTests
    {
        private readonly string _defaultTaskDescription = "task description";

        [Fact]
        public void Create_ValidTaskDescription_ReturnsInstance()
        {
            var taskDescription = TaskDescription.Create(_defaultTaskDescription);

            taskDescription.Value.Should().Be(_defaultTaskDescription);
        }

        [Fact]
        public void Create_TaskDescription_With_Numbers_ReturnsInstance()
        {
            var taskDescription = TaskDescription.Create("Task Description 2025");

            taskDescription.Value.Should().Be("Task Description 2025");
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
            => Assert.Throws<ArgumentOutOfRangeException>(() => TaskDescription.Create(input));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_NullEmptyOrWhitespace_Throws(string input)
            => Assert.Throws<ArgumentException>(() => TaskDescription.Create(input));

        [Fact]
        public void ToString_ReturnsValue()
        {
            var taskDescription = TaskDescription.Create(_defaultTaskDescription);

            taskDescription.ToString().Should().Be(_defaultTaskDescription);
        }

        [Fact]
        public void Equality_SameValue_True()
        {
            var taskDescriptionA = TaskDescription.Create(_defaultTaskDescription);
            var taskDescriptionB = TaskDescription.Create(_defaultTaskDescription);

            taskDescriptionA.Equals(taskDescriptionB).Should().BeTrue();
        }

        [Fact]
        public void Equality_DifferentValue_False()
        {
            var taskDescriptionA = TaskDescription.Create(_defaultTaskDescription);
            var taskDescriptionB = TaskDescription.Create("different task description");

            taskDescriptionA.Equals(taskDescriptionB).Should().BeFalse();
        }

        [Fact]
        public void Equality_IgnoresCase()
        {
            var taskDescriptionA = TaskDescription.Create("First desc");
            var taskDescriptionB = TaskDescription.Create("first desc");

            taskDescriptionA.Should().Be(taskDescriptionB);
            taskDescriptionA.GetHashCode().Should().Be(taskDescriptionB.GetHashCode());
        }

        [Fact]
        public void Operators_Equality_SameValue_True()
        {
            var taskDescriptionA = TaskDescription.Create(_defaultTaskDescription);
            var taskDescriptionB = TaskDescription.Create(_defaultTaskDescription);

            (taskDescriptionA == taskDescriptionB).Should().BeTrue();
            taskDescriptionA.GetHashCode().Should().Be(taskDescriptionB.GetHashCode());
        }

        [Fact]
        public void Operators_Equality_DifferentValue_False()
        {
            var taskDescriptionA = TaskDescription.Create(_defaultTaskDescription);
            var taskDescriptionB = TaskDescription.Create("different task description");

            (taskDescriptionA == taskDescriptionB).Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_SameValue_False()
        {
            var taskDescriptionA = TaskDescription.Create(_defaultTaskDescription);
            var taskDescriptionB = TaskDescription.Create(_defaultTaskDescription);

            (taskDescriptionA != taskDescriptionB).Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_DifferentValue_True()
        {
            var taskDescriptionA = TaskDescription.Create(_defaultTaskDescription);
            var taskDescriptionB = TaskDescription.Create("different task description");

            (taskDescriptionA != taskDescriptionB).Should().BeTrue();
        }

        [Fact]
        public void Operators_Handle_Nulls()
        {
            TaskDescription? taskDescriptionA = null;
            TaskDescription? taskDescriptionB = null;
            var taskDescriptionC = TaskDescription.Create(_defaultTaskDescription);

            (taskDescriptionA == taskDescriptionB).Should().BeTrue();
            (taskDescriptionA == taskDescriptionC).Should().BeFalse();
            (taskDescriptionC != null).Should().BeTrue();
        }

        [Fact]
        public void Implicit_ToString_Works()
        {
            TaskDescription taskDescription = TaskDescription.Create(_defaultTaskDescription);
            string str = taskDescription;

            str.Should().Be(_defaultTaskDescription);
        }
    }
}
