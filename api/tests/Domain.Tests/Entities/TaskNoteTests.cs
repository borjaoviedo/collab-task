using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.Tests.Entities
{
    public sealed class TaskNoteTests
    {
        [Fact]
        public void Set_All_Core_Properties_Assigns_Correctly()
        {
            var taskId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var content = NoteContent.Create("note content here");

            var n = TaskNote.Create(taskId, userId, content);

            n.TaskId.Should().Be(taskId);
            n.UserId.Should().Be(userId);
            n.Content.Should().Be(content);
        }

        [Fact]
        public void TaskNote_Id_Is_Initialized()
        {
            var n = TaskNote.Create(Guid.NewGuid(), Guid.NewGuid(), NoteContent.Create("note content here"));
            n.Id.Should().NotBeEmpty();
            n.Id.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public void Create_Throws_When_TaskId_Is_Empty()
        {
            Action act = () => TaskNote.Create(Guid.Empty, Guid.NewGuid(), NoteContent.Create("note content here"));
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_Throws_When_UserId_Is_Empty()
        {
            Action act = () => TaskNote.Create(Guid.NewGuid(), Guid.Empty, NoteContent.Create("note content here"));
            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("n")]
        [InlineData("  n ")]
        public void Create_Throws_When_Invalid_NoteContent(string input)
        {
            Action act = () => TaskNote.Create(Guid.NewGuid(), Guid.Empty, NoteContent.Create(input));
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Edit_Changes_Content_When_Different()
        {
            var n = TaskNote.Create(Guid.NewGuid(), Guid.NewGuid(), NoteContent.Create("note content here"));
            var newContent = NoteContent.Create("new content");
            n.Edit(newContent);
            n.Content.Should().Be(newContent);
        }

        [Fact]
        public void Edit_Does_Not_Change_NoteContent_When_Same()
        {
            var content = NoteContent.Create("note content here");
            var n = TaskNote.Create(Guid.NewGuid(), Guid.NewGuid(), content);
            n.Edit(content);
            n.Content.Should().Be(content);
        }

        [Fact]
        public void Edit_Throws_When_Invalid_NoteContent()
        {
            var n = TaskNote.Create(Guid.NewGuid(), Guid.NewGuid(), NoteContent.Create("note content here"));

            Action act = () => n.Edit(NoteContent.Create(""));
            act.Should().Throw<ArgumentException>();
        }
    }
}
