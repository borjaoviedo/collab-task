using Application.Columns.DTOs;
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
            var e = Column.Create(
                projectId: Guid.NewGuid(),
                laneId: Guid.NewGuid(),
                name: ColumnName.Create("Todo"),
                order: 1);
            e.GetType().GetProperty("Id")!.SetValue(e, Guid.NewGuid());
            e.GetType().GetProperty("RowVersion")!.SetValue(e, new byte[] { 1, 2 });

            var dto = e.ToReadDto();

            dto.Id.Should().Be(e.Id);
            dto.ProjectId.Should().Be(e.ProjectId);
            dto.LaneId.Should().Be(e.LaneId);
            dto.Name.Should().Be(e.Name.Value);
            dto.Order.Should().Be(e.Order);
            dto.RowVersion.Should().Equal(e.RowVersion);
        }
    }
}
