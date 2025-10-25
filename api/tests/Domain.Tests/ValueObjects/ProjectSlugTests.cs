using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.Tests.ValueObjects
{
    public class ProjectSlugTests
    {
        [Fact]
        public void Create_ValidProjectSlug_ReturnsInstance()
        {
            var pSlug = ProjectSlug.Create("project slug");
            pSlug.Value.Should().Be("project-slug");
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
        public void Create_NullEmptyOrWhitespace_Throws(string? input)
            => Assert.Throws<ArgumentException>(() => ProjectSlug.Create(input!));

        [Fact]
        public void ToString_ReturnsValue()
            => ProjectSlug.Create("a-project-name").ToString().Should().Be("a-project-name");

        [Fact]
        public void ToString_ReturnsNormalizedValue()
            => ProjectSlug.Create("-A_Project #/ Name!").ToString().Should().Be("a-project-name");

        [Fact]
        public void Equality_SameValue_True()
        {
            var a = ProjectSlug.Create("Same Name");
            var b = ProjectSlug.Create("Same Name");
            a.Equals(b).Should().BeTrue();
        }

        [Fact]
        public void Equality_IgnoresCase()
        {
            var a = ProjectSlug.Create("Project Name");
            var b = ProjectSlug.Create("project name");
            a.Equals(b).Should().BeTrue();
            a.GetHashCode().Should().Be(b.GetHashCode());
        }

        [Fact]
        public void Equals_Object_Overload_Works()
        {
            var a = ProjectSlug.Create("Board A");
            object b = ProjectSlug.Create("board a");
            a.Equals(b).Should().BeTrue();
        }

        [Fact]
        public void GetHashCode_SameValue_SameHash()
        {
            var a = ProjectSlug.Create("Same Name");
            var b = ProjectSlug.Create("same name");
            a.GetHashCode().Should().Be(b.GetHashCode());
        }

        [Fact]
        public void Operators_Equality_SameValue_True()
        {
            var a = ProjectSlug.Create("same project name value");
            var b = ProjectSlug.Create("same project name value");

            var result = a == b;
            result.Should().BeTrue();
            a.GetHashCode().Should().Be(b.GetHashCode());
        }

        [Fact]
        public void Operators_Equality_DifferentValue_False()
        {
            var a = ProjectSlug.Create("project name value");
            var b = ProjectSlug.Create("not same project name value");

            var result = a == b;
            result.Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_SameValue_False()
        {
            var a = ProjectSlug.Create("same project name value");
            var b = ProjectSlug.Create("same project name value");

            var result = a != b;
            result.Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_DifferentValue_True()
        {
            var a = ProjectSlug.Create("a project name");
            var b = ProjectSlug.Create("different project name");

            var result = a != b;
            result.Should().BeTrue();
        }

        [Fact]
        public void Operators_Handle_Nulls()
        {
            ProjectSlug? a = null;
            ProjectSlug? b = null;
            (a == b).Should().BeTrue();
            var c = ProjectSlug.Create("Random Name");
            (a == c).Should().BeFalse();
            (c != null).Should().BeTrue();
        }

        [Fact]
        public void Implicit_ToString_Works()
        {
            ProjectSlug e = ProjectSlug.Create("project");
            string s = e;
            s.Should().Be("project");
        }

        [Fact]
        public void ImplicitCast_EqualsToString()
        {
            ProjectSlug e = ProjectSlug.Create("project");
            string s = e;
            e.ToString().Should().Be(s);
        }
    }
}
