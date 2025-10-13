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
            var e = TaskAssignment.AssignOwner(Guid.NewGuid(), Guid.NewGuid());
            e.GetType().GetProperty("RowVersion")!.SetValue(e, new byte[] { 4 });

            var read = e.ToReadDto();
            read.TaskId.Should().Be(e.TaskId);
            read.UserId.Should().Be(e.UserId);
            read.Role.Should().Be(e.Role);
            read.RowVersion.Should().Equal(e.RowVersion);
        }
    }
}
