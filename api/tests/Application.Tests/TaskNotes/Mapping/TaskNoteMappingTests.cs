using Application.TaskNotes.Mapping;
using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;
using TestHelpers.Common.Testing;

namespace Application.Tests.TaskNotes.Mapping
{
    [UnitTest]
    public sealed class TaskNoteMappingTests
    {
        [Fact]
        public void Entity_To_ReadDto_Reflects_Optional_UpdatedAt()
        {
            var entity = TaskNote.Create(
                taskId: Guid.NewGuid(),
                userId: Guid.NewGuid(),
                NoteContent.Create("cotent"));
            entity.GetType().GetProperty("Id")!.SetValue(entity, Guid.NewGuid());
            entity.GetType().GetProperty("RowVersion")!.SetValue(entity, new byte[] { 2 });

            var read = entity.ToReadDto();
            read.Id.Should().Be(entity.Id);
            read.TaskId.Should().Be(entity.TaskId);
            read.UserId.Should().Be(entity.UserId);
            read.Content.Should().Be("cotent");
            read.CreatedAt.Should().Be(entity.CreatedAt);
            read.UpdatedAt.Should().Be(entity.UpdatedAt); // null initially
        }
    }
}
