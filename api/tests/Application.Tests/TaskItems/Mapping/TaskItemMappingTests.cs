using Application.TaskItems.Mapping;
using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;

namespace Application.Tests.TaskItems.Mapping
{
    public sealed class TaskItemMappingTests
    {
        [Fact]
        public void Entity_To_ReadDto_Maps_All()
        {
            var entity = TaskItem.Create(
                columnId: Guid.NewGuid(),
                laneId: Guid.NewGuid(),
                projectId: Guid.NewGuid(),
                title: TaskTitle.Create("Title"),
                description: TaskDescription.Create("Description"),
                dueDate: DateTimeOffset.UtcNow.AddDays(2),
                sortKey: 10m);
            entity.GetType().GetProperty("Id")!.SetValue(entity, Guid.NewGuid());
            entity.GetType().GetProperty("RowVersion")!.SetValue(entity, new byte[] { 7 });

            var dto = entity.ToReadDto();
            dto.Id.Should().Be(entity.Id);
            dto.ProjectId.Should().Be(entity.ProjectId);
            dto.LaneId.Should().Be(entity.LaneId);
            dto.ColumnId.Should().Be(entity.ColumnId);
            dto.Title.Should().Be(entity.Title.Value);
            dto.Description.Should().Be(entity.Description.Value);
            dto.DueDate.Should().Be(entity.DueDate);
            dto.SortKey.Should().Be(entity.SortKey);
            dto.RowVersion.Should().Equal(entity.RowVersion);
        }
    }
}
