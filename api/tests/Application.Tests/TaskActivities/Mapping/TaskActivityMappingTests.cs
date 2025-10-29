using Application.TaskActivities.Mapping;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using TestHelpers.Common.Time;

namespace Application.Tests.TaskActivities.Mapping
{
    public sealed class TaskActivityMappingTests
    {
        [Fact]
        public void Entity_To_ReadDto_Maps_With_Timestamps()
        {
            var entity = TaskActivity.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                TaskActivityType.TaskCreated,
                ActivityPayload.Create("{\"a\":1}"),
                createdAt: TestTime.FixedNow);
            entity.GetType().GetProperty("Id")!.SetValue(entity, Guid.NewGuid());

            var read = entity.ToReadDto();
            read.Id.Should().Be(entity.Id);
            read.TaskId.Should().Be(entity.TaskId);
            read.Type.Should().Be(entity.Type);
            read.Payload.Should().Be(entity.Payload.Value);
            read.CreatedAt.Should().Be(entity.CreatedAt);
        }
    }
}
