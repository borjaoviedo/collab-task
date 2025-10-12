using Application.TaskAssignments.DTOs;
using Application.TaskAssignments.Mapping;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;

namespace Application.Tests.TaskAssignments.Mapping
{
    public sealed class TaskAssignmentMappingTests
    {
        [Fact]
        public void Entity_To_ReadDto_And_Back_To_DeleteDto()
        {
            var e = TaskAssignment.AssignOwner(Guid.NewGuid(), Guid.NewGuid());
            e.GetType().GetProperty("RowVersion")!.SetValue(e, new byte[] { 4 });

            var read = e.ToReadDto();
            read.TaskId.Should().Be(e.TaskId);
            read.UserId.Should().Be(e.UserId);
            read.Role.Should().Be(e.Role);
            read.RowVersion.Should().Equal(e.RowVersion);

            var del = read.ToDeleteDto();
            del.TaskId.Should().Be(read.TaskId);
            del.UserId.Should().Be(read.UserId);
            del.RowVersion.Should().Equal(read.RowVersion);
        }

        [Fact]
        public void CreateDto_To_Entity_Maps()
        {
            var dto = new TaskAssignmentCreateDto
            {
                TaskId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Role = TaskRole.CoOwner
            };

            var e = dto.ToEntity();
            e.TaskId.Should().Be(dto.TaskId);
            e.UserId.Should().Be(dto.UserId);
            e.Role.Should().Be(dto.Role);
        }
    }
}
