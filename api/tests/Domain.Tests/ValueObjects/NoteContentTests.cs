using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.Tests.ValueObjects
{
    public sealed class NoteContentTests
    {
        private readonly string _defaultNoteContent = "note content";

        [Fact]
        public void Create_ValidNoteContent_ReturnsInstance()
        {
            var noteContent = NoteContent.Create(_defaultNoteContent);

            noteContent.Value.Should().Be(_defaultNoteContent);
        }

        [Fact]
        public void Create_NoteContent_With_Numbers_ReturnsInstance()
        {
            var noteContent = NoteContent.Create("Note with numbers 123");

            noteContent.Value.Should().Be("Note with numbers 123");
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

            Assert.Throws<ArgumentOutOfRangeException>(() => NoteContent.Create(new string(chars)));
        }

        [Theory]
        [InlineData("T")]
        [InlineData(" t ")]
        public void Create_TooShortNoteContent_Throws(string input)
            => Assert.Throws<ArgumentOutOfRangeException>(() => NoteContent.Create(input));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_NullEmptyOrWhitespace_Throws(string input)
            => Assert.Throws<ArgumentException>(() => NoteContent.Create(input));

        [Fact]
        public void ToString_ReturnsValue()
        {
            var noteContent = NoteContent.Create(_defaultNoteContent);

            noteContent.ToString().Should().Be(_defaultNoteContent);
        }

        [Fact]
        public void Equality_SameValue_True()
        {
            var noteContentA = NoteContent.Create(_defaultNoteContent);
            var noteContentB = NoteContent.Create(_defaultNoteContent);

            noteContentA.Equals(noteContentB).Should().BeTrue();
        }

        [Fact]
        public void Equality_DifferentValue_False()
        {
            var noteContentA = NoteContent.Create(_defaultNoteContent);
            var noteContentB = NoteContent.Create("different note content");

            noteContentA.Equals(noteContentB).Should().BeFalse();
        }

        [Fact]
        public void Equality_IgnoresCase()
        {
            var noteContentA = NoteContent.Create("First Note");
            var noteContentB = NoteContent.Create("first note");

            noteContentA.Should().Be(noteContentB);
            noteContentA.GetHashCode().Should().Be(noteContentB.GetHashCode());
        }

        [Fact]
        public void Operators_Equality_SameValue_True()
        {
            var noteContentA = NoteContent.Create(_defaultNoteContent);
            var noteContentB = NoteContent.Create(_defaultNoteContent);

            (noteContentA == noteContentB).Should().BeTrue();
            noteContentA.GetHashCode().Should().Be(noteContentB.GetHashCode());
        }

        [Fact]
        public void Operators_Equality_DifferentValue_False()
        {
            var noteContentA = NoteContent.Create(_defaultNoteContent);
            var noteContentB = NoteContent.Create("different note content");

            (noteContentA == noteContentB).Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_SameValue_False()
        {
            var noteContentA = NoteContent.Create(_defaultNoteContent);
            var noteContentB = NoteContent.Create(_defaultNoteContent);

            (noteContentA != noteContentB).Should().BeFalse();
        }

        [Fact]
        public void Operators_Inequality_DifferentValue_True()
        {
            var noteContentA = NoteContent.Create(_defaultNoteContent);
            var noteContentB = NoteContent.Create("different note content");

            (noteContentA != noteContentB).Should().BeTrue();
        }

        [Fact]
        public void Operators_Handle_Nulls()
        {
            NoteContent? noteContentA = null;
            NoteContent? noteContentB = null;
            var noteContentC = NoteContent.Create(_defaultNoteContent);

            (noteContentA == noteContentB).Should().BeTrue();
            (noteContentA == noteContentC).Should().BeFalse();
            (noteContentC != null).Should().BeTrue();
        }

        [Fact]
        public void Implicit_ToString_Works()
        {
            NoteContent noteContent = NoteContent.Create(_defaultNoteContent);
            string str = noteContent;

            str.Should().Be(_defaultNoteContent);
        }
    }
}
