using Application.TaskNotes.Mapping;
using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;

namespace Application.Tests.TaskNotes.Mapping
{
    public sealed class TaskNoteMappingTests
    {
        [Fact]
        public void Entity_To_ReadDto_Reflects_Optional_UpdatedAt()
        {
            var e = TaskNote.Create(Guid.NewGuid(), Guid.NewGuid(), NoteContent.Create("cotent"));
            e.GetType().GetProperty("Id")!.SetValue(e, Guid.NewGuid());
            e.GetType().GetProperty("RowVersion")!.SetValue(e, new byte[] { 2 });

            var read = e.ToReadDto();
            read.Id.Should().Be(e.Id);
            read.TaskId.Should().Be(e.TaskId);
            read.AuthorId.Should().Be(e.AuthorId);
            read.Content.Should().Be("cotent");
            read.CreatedAt.Should().Be(e.CreatedAt);
            read.UpdatedAt.Should().Be(e.UpdatedAt); // null initially
            read.RowVersion.Should().Equal(e.RowVersion);
        }
    }
}
