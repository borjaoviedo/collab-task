using Domain.Entities;
using Domain.Enums;
using FluentAssertions;

namespace Domain.Tests.Entities
{
    public sealed class TaskAssignmentTests
    {
        [Fact]
        public void Create_Valid_TaskAssignment_Assigns_Properties_Correctly()
        {
            var taskId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var role = TaskRole.Owner;
            var assignment = TaskAssignment.Create(taskId, userId, role);

            assignment.TaskId.Should().Be(taskId);
            assignment.UserId.Should().Be(userId);
            assignment.Role.Should().Be(role);
        }

        [Fact]
        public void Create_With_Invalid_TaskId_Throws()
        {
            Action act = () => TaskAssignment.Create(Guid.Empty, Guid.NewGuid(), TaskRole.Owner);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_With_Invalid_UserId_Throws()
        {
            Action act = () => TaskAssignment.Create(Guid.NewGuid(), Guid.Empty, TaskRole.Owner);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void AssignOwner_Creates_Assignment_With_Owner_Role()
        {
            var taskId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var assignment = TaskAssignment.AssignOwner(taskId, userId);
            assignment.TaskId.Should().Be(taskId);
            assignment.UserId.Should().Be(userId);
            assignment.Role.Should().Be(TaskRole.Owner);
        }

        [Fact]
        public void AssignCoOwner_Creates_Assignment_With_CoOwner_Role()
        {
            var taskId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var assignment = TaskAssignment.AssignCoOwner(taskId, userId);
            assignment.TaskId.Should().Be(taskId);
            assignment.UserId.Should().Be(userId);
            assignment.Role.Should().Be(TaskRole.CoOwner);
        }

        [Fact]
        public void Multiple_Assignments_Can_Be_Created_Independently()
        {
            var taskId = Guid.NewGuid();
            var userId1 = Guid.NewGuid();
            var userId2 = Guid.NewGuid();
            var ownerAssignment = TaskAssignment.AssignOwner(taskId, userId1);
            var coOwnerAssignment = TaskAssignment.AssignCoOwner(taskId, userId2);

            ownerAssignment.TaskId.Should().Be(taskId);
            ownerAssignment.UserId.Should().Be(userId1);
            ownerAssignment.Role.Should().Be(TaskRole.Owner);

            coOwnerAssignment.TaskId.Should().Be(taskId);
            coOwnerAssignment.UserId.Should().Be(userId2);
            coOwnerAssignment.Role.Should().Be(TaskRole.CoOwner);
        }
    }
}
