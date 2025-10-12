using Application.TaskActivities.DTOs;
using Application.TaskActivities.Mapping;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;

namespace Application.Tests.TaskActivities.Mapping
{
    public sealed class TaskActivityMappingTests
    {
        [Fact]
        public void CreateDto_To_Entity_Maps()
        {
            var dto = new TaskActivityCreateDto
            {
                TaskId = Guid.NewGuid(),
                ActorId = Guid.NewGuid(),
                Type = TaskActivityType.TaskEdited,
                Payload = "{\"k\":1}"
            };

            var e = dto.ToEntity();
            e.TaskId.Should().Be(dto.TaskId);
            e.Type.Should().Be(dto.Type);
            e.Payload.Value.Should().Be(dto.Payload);
        }

        [Fact]
        public void Entity_To_ReadDto_Maps_With_Timestamps()
        {
            var e = TaskActivity.Create(Guid.NewGuid(), Guid.NewGuid(), TaskActivityType.TaskCreated, ActivityPayload.Create("{\"a\":1}"));
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
