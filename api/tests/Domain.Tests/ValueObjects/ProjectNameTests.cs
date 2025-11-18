using Domain.ValueObjects;
using FluentAssertions;
using TestHelpers.Common.Testing;

namespace Domain.Tests.ValueObjects
{
    [UnitTest]
    public class ProjectNameTests
    {
        private readonly string _defaultProjectName = "project name";

        [Fact]
        public void Create_ValidProjectName_ReturnsInstance()
        {
            var projectName = ProjectName.Create(_defaultProjectName);

            projectName.Value.Should().Be(_defaultProjectName);
        }

        [Fact]
        public void Create_MinLength1_Passes()
            => ProjectName.Create("w").Value.Should().HaveLength(1);

        [Fact]
        public void Create_MaxLength100_Passes()
            => ProjectName.Create(new string('x', 100)).Value.Should().HaveLength(100);

        [Fact]
        public void Create_Trim_DoesNotCount_Towards_Length()
        {
            var input = "  " + new string('x', 100) + "  ";
            var projectName = ProjectName.Create(input);

            projectName.Value.Should().HaveLength(100);
        }

        [Fact]
        public void Create_TooLongProjectName_Throws()
        {
            var random = new Random();
            var chars = Enumerable.Range(0, 101)
                .Select(_ => (char)random.Next('a', 'z' + 1))
                .ToArray();
            var tooLong = new string(chars);

            Assert.Throws<ArgumentOutOfRangeException>(() => ProjectName.Create(tooLong));
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("   not trimmed projec name   ")]
        public void Create_Trim_Applied_Correctly(string input)
        {
            var trimmedInput = input.Trim();

            if (trimmedInput.Length == 0)
                Assert.Throws<ArgumentException>(() => ProjectName.Create(input));
            else
                ProjectName.Create(input).Value.Should().Be(trimmedInput);
        }

        [Theory]
        [InlineData("Dashed-Project-Name")]
        [InlineData("Under_Scored_Project_Name")]
        [InlineData("Dotted.Project.Name")]
        public void Create_Allows_Common_LocalChars(string input)
            => ProjectName.Create(input).Value.Should().Be(input);

        [Theory]
        [InlineData("Proyéctó Ñame", "Proyéctó Ñame")]
        [InlineData("NAÏVE NAME", "NAÏVE NAME")]
        public void Create_Preserves_Unicode(string input, string expected)
            => ProjectName.Create(input).Value.Should().Be(expected);

        [Theory]
        [InlineData("PROJECT NAME", "PROJECT NAME")]
        [InlineData("  Mixed Case  ", "Mixed Case")]
        public void Create_Normalizes_Trim_And_Maintains_Cases(string input, string expected)
            => ProjectName.Create(input).Value.Should().Be(expected);

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_NullEmptyOrWhitespace_Throws(string? input)
            => Assert.Throws<ArgumentException>(() => ProjectName.Create(input!));

        [Fact]
        public void ToString_ReturnsValue()
        {
            var projectName = ProjectName.Create(_defaultProjectName);

            projectName.ToString().Should().Be(_defaultProjectName);
        }

        [Fact]
        public void Equality_SameValue_True()
        {
            var projectNameA = ProjectName.Create(_defaultProjectName);
            var projectNameB = ProjectName.Create(_defaultProjectName);

            projectNameA.Equals(projectNameB).Should().BeTrue();
        }

        [Fact]
        public void Equality_DifferentValue_False()
        {
            var projectNameA = ProjectName.Create(_defaultProjectName);
            var projectNameB = ProjectName.Create("different project name");

            projectNameA.Equals(projectNameB).Should().BeFalse();
        }

        [Fact]
        public void Equality_IgnoresCase()
        {
            var projectNameA = ProjectName.Create("Project Name");
            var projectNameB = ProjectName.Create("project name");

            projectNameA.Should().Be(projectNameB);
            projectNameA.GetHashCode().Should().Be(projectNameB.GetHashCode());
        }

        [Fact]
        public void Equals_Object_Overload_Works()
        {
            var projectNameA = ProjectName.Create(_defaultProjectName);
            object projectNameB = ProjectName.Create(_defaultProjectName);

            projectNameA.Equals(projectNameB).Should().BeTrue();
        }

        [Fact]
        public void GetHashCode_SameValue_SameHash()
        {
            var projectNameA = ProjectName.Create(_defaultProjectName);
            var projectNameB = ProjectName.Create(_defaultProjectName);

            projectNameA.GetHashCode().Should().Be(projectNameB.GetHashCode());
        }

        [Fact]
        public void Operators_Equality_SameValue_True()
        {
            var projectNameA = ProjectName.Create(_defaultProjectName);
            var projectNameB = ProjectName.Create(_defaultProjectName);

            (projectNameA == projectNameB).Should().BeTrue();
            projectNameA.GetHashCode().Should().Be(projectNameB.GetHashCode());
        }

        [Fact]
        public void Operators_Equality_DifferentValue_False()
        {
            var projectNameA = ProjectName.Create(_defaultProjectName);
            var projectNameB = ProjectName.Create("different project name");

            (projectNameA == projectNameB).Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_SameValue_False()
        {
            var projectNameA = ProjectName.Create(_defaultProjectName);
            var projectNameB = ProjectName.Create(_defaultProjectName);

            (projectNameA != projectNameB).Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_DifferentValue_True()
        {
            var projectNameA = ProjectName.Create(_defaultProjectName);
            var projectNameB = ProjectName.Create("different project name");

            (projectNameA != projectNameB).Should().BeTrue();
        }

        [Fact]
        public void Operators_Handle_Nulls()
        {
            ProjectName? projectNameA = null;
            ProjectName? projectNameB = null;
            var projectNameC = ProjectName.Create(_defaultProjectName);

            (projectNameA == projectNameB).Should().BeTrue();
            (projectNameA == projectNameC).Should().BeFalse();
            (projectNameC != null).Should().BeTrue();
        }

        [Fact]
        public void Implicit_ToString_Works()
        {
            ProjectName projectName = ProjectName.Create(_defaultProjectName);
            string str = projectName;

            str.Should().Be(_defaultProjectName);
        }
    }
}
