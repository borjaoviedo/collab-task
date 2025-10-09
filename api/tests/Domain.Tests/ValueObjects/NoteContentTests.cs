using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.Tests.ValueObjects
{
    public sealed class NoteContentTests
    {
        [Fact]
        public void Create_ValidNoteContent_ReturnsInstance()
        {
            var n = NoteContent.Create("This is a note");
            n.Value.Should().Be("This is a note");
        }

        [Fact]
        public void Create_NoteContent_With_Numbers_ReturnsInstance()
        {
            var n = NoteContent.Create("Note with numbers 123");
            n.Value.Should().Be("Note with numbers 123");
        }

        [Theory]
        [InlineData("New_note")]
        [InlineData("New-note.")]
        public void Create_Allows_Common_LocalChars(string input)
            => NoteContent.Create(input).Value.Should().Be(input);

        [Fact]
        public void Create_MinLength_Passes()
            => NoteContent.Create("new").Value.Should().HaveLength(3);

        [Fact]
        public void Create_MaxLength500_Passes()
            => NoteContent.Create(new string('x', 500)).Value.Should().HaveLength(500);

        [Theory]
        [InlineData("  ")]
        [InlineData("   Not trimmed   ")]
        public void Create_Trim_Applied_Correctly(string input)
        {
            var trimmedInput = input.Trim();
            if (trimmedInput.Length == 0)
                Assert.Throws<ArgumentException>(() => NoteContent.Create(input));
            else
                NoteContent.Create(input).Value.Should().Be(trimmedInput);
        }

        [Theory]
        [InlineData("Note  Content")]
        [InlineData("New  Valid  Note")]
        [InlineData("Valid      content")]
        public void Create_NoteContent_With_Two_Or_More_Consecutive_Spaces_Passes(string input)
            => NoteContent.Create(input).Value.Should().Be(input);

        [Theory]
        [InlineData("nötë cóntént", "nötë cóntént")]
        [InlineData("naüve çòntènt", "naüve çòntènt")]
        public void Create_Preserves_Unicode(string input, string expected)
            => NoteContent.Create(input).Value.Should().Be(expected);

        [Theory]
        [InlineData("NOTE CONTENT", "NOTE CONTENT")]
        [InlineData("  Mixed Case  ", "Mixed Case")]
        public void Create_Normalizes_Trim_And_Maintains_Cases(string input, string expected)
            => NoteContent.Create(input).Value.Should().Be(expected);

        [Fact]
        public void Create_TooLongNoteContent_Throws()
        {
            var random = new Random();
            var chars = Enumerable.Range(0, 501)
                .Select(_ => (char)random.Next('a', 'z' + 1))
                .ToArray();

            Assert.Throws<ArgumentException>(() => NoteContent.Create(new string(chars)));
        }

        [Theory]
        [InlineData("T")]
        [InlineData(" t ")]
        public void Create_TooShortNoteContent_Throws(string input)
        {
            Assert.Throws<ArgumentException>(() => NoteContent.Create(input));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_NullEmptyOrWhitespace_Throws(string? input)
            => Assert.Throws<ArgumentException>(() => NoteContent.Create(input!));

        [Fact]
        public void ToString_ReturnsValue()
            => NoteContent.Create("note content").ToString().Should().Be("note content");

        [Fact]
        public void Equality_SameValue_True()
        {
            var a = NoteContent.Create("same note");
            var b = NoteContent.Create("same note");
            a.Equals(b).Should().BeTrue();
        }

        [Fact]
        public void Equality_DifferentValue_False()
        {
            var a = NoteContent.Create("first note");
            var b = NoteContent.Create("second note");
            a.Equals(b).Should().BeFalse();
        }

        [Fact]
        public void Equality_IgnoresCase()
        {
            var a = NoteContent.Create("First Note");
            var b = NoteContent.Create("first note");
            a.Should().Be(b);
            a.GetHashCode().Should().Be(b.GetHashCode());
        }

        [Fact]
        public void Operators_Equality_SameValue_True()
        {
            var a = NoteContent.Create("Same note");
            var b = NoteContent.Create("Same note");

            var result = a == b;
            result.Should().BeTrue();
            a.GetHashCode().Should().Be(b.GetHashCode());
        }

        [Fact]
        public void Operators_Equality_DifferentValue_False()
        {
            var a = NoteContent.Create("Same note");
            var b = NoteContent.Create("Not same note");

            var result = a == b;
            result.Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_SameValue_False()
        {
            var a = NoteContent.Create("Same note");
            var b = NoteContent.Create("Same note");

            var result = a != b;
            result.Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_DifferentValue_True()
        {
            var a = NoteContent.Create("Same note");
            var b = NoteContent.Create("Not note");

            var result = a != b;
            result.Should().BeTrue();
        }

        [Fact]
        public void Operators_Handle_Nulls()
        {
            NoteContent? a = null;
            NoteContent? b = null;
            (a == b).Should().BeTrue();
            var c = NoteContent.Create("note");
            (a == c).Should().BeFalse();
            (c != null).Should().BeTrue();
        }

        [Fact]
        public void Implicit_ToString_Works()
        {
            NoteContent n = NoteContent.Create("note");
            string s = n;
            s.Should().Be("note");
        }
    }
}
