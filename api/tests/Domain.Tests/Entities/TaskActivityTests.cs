using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using TestHelpers.Time;

namespace Domain.Tests.Entities
{
    public sealed class TaskActivityTests
    {
        [Fact]
        public void Create_ValidInputs_SetsProperties()
        {
            var taskId = Guid.NewGuid();
            var actorId = Guid.NewGuid();
            var type = TaskActivityType.TaskCreated;
            var payload = ActivityPayload.Create("{\"title\":\"T1\"}");
            var activity = TaskActivity.Create(
                taskId,
                actorId,
                type,
                payload,
                createdAt: TestTime.FixedNow);

            activity.Id.Should().NotBeEmpty();
            activity.TaskId.Should().Be(taskId);
            activity.ActorId.Should().Be(actorId);
            activity.Type.Should().Be(type);
            activity.Payload.Should().Be(payload);
        }

        [Fact]
        public void Create_Empty_TaskId_Throws()
        {
            Action act = () => TaskActivity.Create(
                Guid.Empty,
                Guid.NewGuid(),
                TaskActivityType.TaskEdited,
                ActivityPayload.Create("{\"x\":1}"),
                createdAt: TestTime.FixedNow);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_Empty_ActorId_Throws()
        {
            Action act = () => TaskActivity.Create(
                Guid.NewGuid(),
                Guid.Empty,
                TaskActivityType.TaskMoved,
                ActivityPayload.Create("{\"x\":1}"),
                createdAt: TestTime.FixedNow);
            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("{ a: 1 ")]
        public void Create_Invalid_Payload_Throws(string input)
        {
            Action act = () => TaskActivity.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                TaskActivityType.TaskMoved,
                ActivityPayload.Create(input),
                createdAt: TestTime.FixedNow);
            act.Should().Throw<ArgumentException>();
        }
    }
}
