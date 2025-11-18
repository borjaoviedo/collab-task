using Application.Columns.Mapping;
using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;

namespace Application.Tests.Columns.Mapping
{
    public sealed class ColumnMappingTests
    {
        [Fact]
        public void ToReadDto_Maps_All_Fields()
        {
            var entity = Column.Create(
                projectId: Guid.NewGuid(),
                laneId: Guid.NewGuid(),
                name: ColumnName.Create("Todo"),
                order: 1);
            entity.GetType().GetProperty("Id")!.SetValue(entity, Guid.NewGuid());
            entity.GetType().GetProperty("RowVersion")!.SetValue(entity, new byte[] { 1, 2 });

            var dto = entity.ToReadDto();

            dto.Id.Should().Be(entity.Id);
            dto.ProjectId.Should().Be(entity.ProjectId);
            dto.LaneId.Should().Be(entity.LaneId);
            dto.Name.Should().Be(entity.Name.Value);
            dto.Order.Should().Be(entity.Order);
        }
    }
}
