using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.Tests.Entities
{
    public sealed class TaskNoteTests
    {
        private static readonly Guid _defaultTaskId = Guid.NewGuid();
        private static readonly Guid _defaultUserId = Guid.NewGuid();
        private static readonly NoteContent _defaultNoteContent = NoteContent.Create("note content");

        private readonly TaskNote _defaultTaskNote = TaskNote.Create(
            _defaultTaskId,
            _defaultUserId,
            _defaultNoteContent);

        [Fact]
        public void Set_All_Core_Properties_Assigns_Correctly()
        {
            var note = _defaultTaskNote;

            note.TaskId.Should().Be(_defaultTaskId);
            note.UserId.Should().Be(_defaultUserId);
            note.Content.Should().Be(_defaultNoteContent);
        }

        [Fact]
        public void TaskNote_Id_Is_Initialized()
        {
            var note = _defaultTaskNote;

            note.Id.Should().NotBeEmpty();
            note.Id.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public void Create_Throws_When_TaskId_Is_Empty()
        {
            var act = () => TaskNote.Create(
                taskId: Guid.Empty,
                _defaultUserId,
                _defaultNoteContent);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_Throws_When_UserId_Is_Empty()
        {
            var act = () => TaskNote.Create(
                _defaultTaskId,
                userId: Guid.Empty,
                _defaultNoteContent);

            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("note")]
        [InlineData("  note ")]
        public void Create_Throws_When_Invalid_NoteContent(string input)
        {
            var act = () => TaskNote.Create(
                _defaultTaskId,
                userId: Guid.Empty,
                content: NoteContent.Create(input));

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Edit_Changes_Content_When_Different()
        {
            var note = _defaultTaskNote;
            var newContent = NoteContent.Create("new content");

            note.Edit(newContent);

            note.Content.Should().Be(newContent);
        }

        [Fact]
        public void Edit_Does_Not_Change_NoteContent_When_Same()
        {
            var note = _defaultTaskNote;

            note.Edit(_defaultNoteContent);

            note.Content.Should().Be(_defaultNoteContent);
        }

        [Fact]
        public void Edit_Throws_When_Invalid_NoteContent()
        {
            var note = _defaultTaskNote;

            var act = () => note.Edit(NoteContent.Create(""));

            act.Should().Throw<ArgumentException>();
        }
    }
}
