using Application.TaskItems.DTOs;
using Application.TaskItems.Mapping;
using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;

namespace Application.Tests.TaskItems.Mapping
{
    public sealed class TaskItemMappingTests
    {
        [Fact]
        public void CreateDto_To_Entity_Maps_All()
        {
            var dto = new TaskItemCreateDto
            {
                ProjectId = Guid.NewGuid(),
                LaneId = Guid.NewGuid(),
                ColumnId = Guid.NewGuid(),
                Title = "Title",
                Description = "Description",
                DueDate = DateTimeOffset.UtcNow.AddDays(1)
            };

            var e = dto.ToEntity();
            e.ProjectId.Should().Be(dto.ProjectId);
            e.LaneId.Should().Be(dto.LaneId);
            e.ColumnId.Should().Be(dto.ColumnId);
            e.Title.Value.Should().Be(dto.Title);
            e.Description.Value.Should().Be(dto.Description);
            e.DueDate.Should().Be(dto.DueDate);
            e.SortKey.Should().Be(0m);
        }

        [Fact]
        public void Entity_To_ReadDto_Maps_All()
        {
            var e = TaskItem.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                TaskTitle.Create("Title"), TaskDescription.Create("Description"), DateTimeOffset.UtcNow.AddDays(2), 10m);
            e.GetType().GetProperty("Id")!.SetValue(e, Guid.NewGuid());
            e.GetType().GetProperty("RowVersion")!.SetValue(e, new byte[] { 7 });

            var dto = e.ToReadDto();
            dto.Id.Should().Be(e.Id);
            dto.ProjectId.Should().Be(e.ProjectId);
            dto.LaneId.Should().Be(e.LaneId);
            dto.ColumnId.Should().Be(e.ColumnId);
            dto.Title.Should().Be(e.Title.Value);
            dto.Description.Should().Be(e.Description.Value);
            dto.DueDate.Should().Be(e.DueDate);
            dto.SortKey.Should().Be(e.SortKey);
            dto.RowVersion.Should().Equal(e.RowVersion);
        }

        [Fact]
        public void MoveDto_To_Update_Assigns_Targets()
        {
            var e = TaskItem.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                TaskTitle.Create("Title"), TaskDescription.Create("Description"), null, 0m);
            var dto = new TaskItemMoveDto
            {
                Id = e.Id,
                TargetLaneId = Guid.NewGuid(),
                TargetColumnId = Guid.NewGuid(),
                TargetSortKey = 123.45m,
                RowVersion = [1]
            };

            e.Move(dto.TargetLaneId, dto.TargetColumnId, dto.TargetSortKey);
            e.LaneId.Should().Be(dto.TargetLaneId);
            e.ColumnId.Should().Be(dto.TargetColumnId);
            e.SortKey.Should().Be(dto.TargetSortKey);
        }
    }
}
