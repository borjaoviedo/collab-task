using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.Tests.ValueObjects
{
    public class ProjectSlugTests
    {
        private readonly string _defaultProjectName = "project name";

        [Fact]
        public void Create_ValidProjectSlug_ReturnsInstance()
        {
            var projectSlug = ProjectSlug.Create(_defaultProjectName);

            projectSlug.Value.Should().Be("project-name");
        }

        [Fact]
        public void Create_MinLength1_Passes()
            => ProjectSlug.Create("p").Value.Should().HaveLength(1);

        [Fact]
        public void Create_MaxLength100_Passes()
            => ProjectSlug.Create(new string('x', 100)).Value.Should().HaveLength(100);

        [Theory]
        [InlineData("  A  ", "a")]
        [InlineData("Collab   Task!! Manager", "collab-task-manager")]
        [InlineData("A---B___C   D///E", "a-b-c-d-e")]
        [InlineData("-A_Project #/ Name!", "a-project-name")]
        public void Normalize_CommonCases(string input, string expected)
            => ProjectSlug.Create(input).Value.Should().Be(expected);

        [Theory]
        [InlineData("Árbol Niño São_Paulo", "arbol-nino-sao-paulo")]
        [InlineData("München", "munchen")]
        [InlineData("François Dupont", "francois-dupont")]
        public void Normalize_RemovesDiacritics(string input, string expected)
            => ProjectSlug.Create(input).Value.Should().Be(expected);

        [Fact]
        public void Normalize_CollapsesRepeatingDashes()
            => ProjectSlug.Create("a---__  ---b").Value.Should().Be("a-b");

        [Theory]
        [InlineData("!!!")]
        [InlineData("___")]
        [InlineData("---")]
        [InlineData("/// ///")]
        public void Create_AllInvalidChars_Throws(string input)
            => Assert.ThrowsAny<ArgumentException>(() => ProjectSlug.Create(input));

        [Fact]
        public void Create_InputWithOnlyDashesAround_StaysAlnumEdges()
            => ProjectSlug.Create("---abc---").Value.Should().Be("abc");

        [Fact]
        public void Create_NonLatinOnly_Throws()
            => Assert.ThrowsAny<ArgumentException>(() => ProjectSlug.Create("计划"));

        [Fact]
        public void Create_LowercasesAlways()
            => ProjectSlug.Create("ProJecT NAME").Value.Should().Be("project-name");

        [Fact]
        public void Create_TooLongProjectSlug_Throws()
        {
            var random = new Random();
            var chars = Enumerable.Range(0, 101)
                .Select(_ => (char)random.Next('a', 'z' + 1))
                .ToArray();
            var tooLong = new string(chars);

            Assert.Throws<ArgumentOutOfRangeException>(() => ProjectSlug.Create(tooLong));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_NullEmptyOrWhitespace_Throws(string input)
            => Assert.Throws<ArgumentException>(() => ProjectSlug.Create(input));

        [Fact]
        public void ToString_ReturnsValue()
        {
            var projectSlugString = "a-project-name";
            var projectSlug = ProjectSlug.Create(projectSlugString);

            projectSlug.ToString().Should().Be(projectSlugString);
        }

        [Fact]
        public void ToString_ReturnsNormalizedValue()
            => ProjectSlug.Create("-A_Project #/ Name!").ToString().Should().Be("a-project-name");

        [Fact]
        public void Equality_SameValue_True()
        {
            var projectSlugA = ProjectSlug.Create(_defaultProjectName);
            var projectSlugB = ProjectSlug.Create(_defaultProjectName);

            projectSlugA.Equals(projectSlugB).Should().BeTrue();
        }

        [Fact]
        public void Equality_IgnoresCase()
        {
            var projectSlugA = ProjectSlug.Create("Project Name");
            var projectSlugB = ProjectSlug.Create("project name");

            projectSlugA.Equals(projectSlugB).Should().BeTrue();
            projectSlugA.GetHashCode().Should().Be(projectSlugB.GetHashCode());
        }

        [Fact]
        public void Equals_Object_Overload_Works()
        {
            var projectSlugA = ProjectSlug.Create(_defaultProjectName);
            object projectSlugB = ProjectSlug.Create(_defaultProjectName);

            projectSlugA.Equals(projectSlugB).Should().BeTrue();
        }

        [Fact]
        public void GetHashCode_SameValue_SameHash()
        {
            var projectSlugA = ProjectSlug.Create(_defaultProjectName);
            var projectSlugB = ProjectSlug.Create(_defaultProjectName);

            projectSlugA.GetHashCode().Should().Be(projectSlugB.GetHashCode());
        }

        [Fact]
        public void Operators_Equality_SameValue_True()
        {
            var projectSlugA = ProjectSlug.Create(_defaultProjectName);
            var projectSlugB = ProjectSlug.Create(_defaultProjectName);

            (projectSlugA == projectSlugB).Should().BeTrue();
            projectSlugA.GetHashCode().Should().Be(projectSlugB.GetHashCode());
        }

        [Fact]
        public void Operators_Equality_DifferentValue_False()
        {
            var projectSlugA = ProjectSlug.Create(_defaultProjectName);
            var projectSlugB = ProjectSlug.Create("different project name");

            (projectSlugA == projectSlugB).Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_SameValue_False()
        {
            var projectSlugA = ProjectSlug.Create(_defaultProjectName);
            var projectSlugB = ProjectSlug.Create(_defaultProjectName);

            (projectSlugA != projectSlugB).Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_DifferentValue_True()
        {
            var projectSlugA = ProjectSlug.Create(_defaultProjectName);
            var projectSlugB = ProjectSlug.Create("different project name");

            (projectSlugA != projectSlugB).Should().BeTrue();
        }

        [Fact]
        public void Operators_Handle_Nulls()
        {
            ProjectSlug? projectSlugA = null;
            ProjectSlug? projectSlugB = null;
            var projectSlugC = ProjectSlug.Create(_defaultProjectName);

            (projectSlugA == projectSlugB).Should().BeTrue();
            (projectSlugA == projectSlugC).Should().BeFalse();
            (projectSlugC != null).Should().BeTrue();
        }

        [Fact]
        public void Implicit_ToString_Works()
        {
            var projectSlugString = "a-project-name";
            ProjectSlug projectSlug = ProjectSlug.Create(projectSlugString);
            string str = projectSlug;

            str.Should().Be(projectSlugString);
        }

        [Fact]
        public void ImplicitCast_EqualsToString()
        {
            ProjectSlug projectSlug = ProjectSlug.Create(_defaultProjectName);
            string str = projectSlug;

            projectSlug.ToString().Should().Be(str);
        }
    }
}
