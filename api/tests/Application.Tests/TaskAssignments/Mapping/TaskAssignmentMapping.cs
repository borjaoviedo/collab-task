using Application.TaskAssignments.Mapping;
using Domain.Entities;
using FluentAssertions;

namespace Application.Tests.TaskAssignments.Mapping
{
    public sealed class TaskAssignmentMappingTests
    {
        [Fact]
        public void Entity_To_ReadDto()
        {
            var entity = TaskAssignment.AssignOwner(taskId: Guid.NewGuid(), userId: Guid.NewGuid());
            entity.GetType().GetProperty("RowVersion")!.SetValue(entity, new byte[] { 4 });

            var read = entity.ToReadDto();
            read.TaskId.Should().Be(entity.TaskId);
            read.UserId.Should().Be(entity.UserId);
            read.Role.Should().Be(entity.Role);
        }
    }
}
