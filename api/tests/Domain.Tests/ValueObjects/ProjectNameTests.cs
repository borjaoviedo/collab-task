using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.Tests.ValueObjects
{
    public class ProjectNameTests
    {
        [Fact]
        public void Create_ValidProjectName_ReturnsInstance()
        {
            var pName = ProjectName.Create("first project name");
            pName.Value.Should().Be("first project name");
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
            var p = ProjectName.Create(input);
            p.Value.Should().HaveLength(100);
        }

        [Fact]
        public void Create_TooLongProjectName_Throws()
        {
            var random = new Random();
            var chars = Enumerable.Range(0, 101)
                .Select(_ => (char)random.Next('a', 'z' + 1))
                .ToArray();
            var tooLong = new string(chars);

            Assert.Throws<ArgumentException>(() => ProjectName.Create(tooLong));
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
            => ProjectName.Create(input).Value.Should().Be(input.ToLowerInvariant());

        [Theory]
        [InlineData("Proyéctó Ñame", "proyéctó ñame")]
        [InlineData("NAÏVE NAME", "naïve name")]
        public void Create_Preserves_Unicode_ToLowerInvariant(string input, string expected)
            => ProjectName.Create(input).Value.Should().Be(expected);

        [Theory]
        [InlineData("PROJECT NAME", "project name")]
        [InlineData("  Mixed Case  ", "mixed case")]
        public void Create_Normalizes_ToLower_And_Trim(string input, string expected)
            => ProjectName.Create(input).Value.Should().Be(expected);

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_NullEmptyOrWhitespace_Throws(string? input)
            => Assert.Throws<ArgumentException>(() => ProjectName.Create(input!));

        [Fact]
        public void ToString_ReturnsValue()
            => ProjectName.Create("a project name").ToString().Should().Be("a project name");

        [Fact]
        public void Equality_SameValue_True()
        {
            var a = ProjectName.Create("Same Name");
            var b = ProjectName.Create("Same Name");
            a.Equals(b).Should().BeTrue();
        }

        [Fact]
        public void Equality_DifferentValue_False()
        {
            var a = ProjectName.Create("Project Name");
            var b = ProjectName.Create("Project  Name");
            a.Equals(b).Should().BeFalse();
        }

        [Fact]
        public void Equality_IgnoresCase()
        {
            var a = ProjectName.Create("Project Name");
            var b = ProjectName.Create("project name");
            a.Should().Be(b);
            a.GetHashCode().Should().Be(b.GetHashCode());
        }

        [Fact]
        public void Equals_Object_Overload_Works()
        {
            var a = ProjectName.Create("Board A");
            object b = ProjectName.Create("board a");
            a.Equals(b).Should().BeTrue();
        }

        [Fact]
        public void GetHashCode_SameValue_SameHash()
        {
            var a = ProjectName.Create("Same Name");
            var b = ProjectName.Create("same name");
            a.GetHashCode().Should().Be(b.GetHashCode());
        }

        [Fact]
        public void Operators_Equality_SameValue_True()
        {
            var a = ProjectName.Create("same project name value");
            var b = ProjectName.Create("same project name value");

            var result = a == b;
            result.Should().BeTrue();
            a.GetHashCode().Should().Be(b.GetHashCode());
        }

        [Fact]
        public void Operators_Equality_DifferentValue_False()
        {
            var a = ProjectName.Create("project name value");
            var b = ProjectName.Create("not same project name value");

            var result = a == b;
            result.Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_SameValue_False()
        {
            var a = ProjectName.Create("same project name value");
            var b = ProjectName.Create("same project name value");

            var result = a != b;
            result.Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_DifferentValue_True()
        {
            var a = ProjectName.Create("a project name");
            var b = ProjectName.Create("different project name");

            var result = a != b;
            result.Should().BeTrue();
        }

        [Fact]
        public void Operators_Handle_Nulls()
        {
            ProjectName? a = null;
            ProjectName? b = null;
            (a == b).Should().BeTrue();
            var c = ProjectName.Create("Random Name");
            (a == c).Should().BeFalse();
            (c != null).Should().BeTrue();
        }

        [Fact]
        public void Implicit_ToString_Works()
        {
            ProjectName e = ProjectName.Create("project");
            string s = e;
            s.Should().Be("project");
        }
    }
}
