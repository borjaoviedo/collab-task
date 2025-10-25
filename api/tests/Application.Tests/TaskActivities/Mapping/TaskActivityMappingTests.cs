using Application.TaskActivities.Mapping;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using TestHelpers.Time;

namespace Application.Tests.TaskActivities.Mapping
{
    public sealed class TaskActivityMappingTests
    {
        [Fact]
        public void Entity_To_ReadDto_Maps_With_Timestamps()
        {
            var e = TaskActivity.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                TaskActivityType.TaskCreated,
                ActivityPayload.Create("{\"a\":1}"),
                createdAt: TestTime.FixedNow);
            e.GetType().GetProperty("Id")!.SetValue(e, Guid.NewGuid());

            var read = e.ToReadDto();
            read.Id.Should().Be(e.Id);
            read.TaskId.Should().Be(e.TaskId);
            read.Type.Should().Be(e.Type);
            read.Payload.Should().Be(e.Payload.Value);
            read.CreatedAt.Should().Be(e.CreatedAt);
        }
    }
}
