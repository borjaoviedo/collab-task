using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using TestHelpers.Common.Testing;
using TestHelpers.Common.Time;

namespace Domain.Tests.Entities
{
    [UnitTest]
    public sealed class TaskActivityTests
    {
        [Fact]
        public void Create_ValidInputs_SetsProperties()
        {
            var taskId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var type = TaskActivityType.TaskCreated;
            var payload = ActivityPayload.Create("{\"title\":\"T1\"}");
            var activity = TaskActivity.Create(
                taskId,
                userId,
                type,
                payload,
                createdAt: TestTime.FixedNow);

            activity.Id.Should().NotBeEmpty();
            activity.TaskId.Should().Be(taskId);
            activity.ActorId.Should().Be(userId);
            activity.Type.Should().Be(type);
            activity.Payload.Should().Be(payload);
        }

        [Fact]
        public void Create_Empty_TaskId_Throws()
        {
            var act = () => TaskActivity.Create(
                taskId: Guid.Empty,
                userId: Guid.NewGuid(),
                type: TaskActivityType.TaskEdited,
                payload: ActivityPayload.Create("{\"x\":1}"),
                createdAt: TestTime.FixedNow);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_Empty_UserId_Throws()
        {
            var act = () => TaskActivity.Create(
                taskId: Guid.NewGuid(),
                userId: Guid.Empty,
                type: TaskActivityType.TaskMoved,
                payload: ActivityPayload.Create("{\"x\":1}"),
                createdAt: TestTime.FixedNow);

            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("{ a: 1 ")]
        public void Create_Invalid_Payload_Throws(string? input)
        {
            var act = () => TaskActivity.Create(
                taskId: Guid.NewGuid(),
                userId: Guid.NewGuid(),
                type: TaskActivityType.TaskMoved,
                payload: ActivityPayload.Create(input!),
                createdAt: TestTime.FixedNow);

            act.Should().Throw<ArgumentException>();
        }
    }
}
