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

        [Fact]
        public void ToEntity_From_CreateDto_Maps_Domain_Correctly()
        {
            var create = new ColumnCreateDto
            {
                ProjectId = Guid.NewGuid(),
                LaneId = Guid.NewGuid(),
                Name = "In Progress",
                Order = 2
            };

            var entity = create.ToEntity();
            entity.ProjectId.Should().Be(create.ProjectId);
            entity.LaneId.Should().Be(create.LaneId);
            entity.Name.Value.Should().Be(create.Name);
            entity.Order.Should().Be(create.Order);
        }

        [Fact]
        public void ToDeleteDto_Uses_Id_And_RowVersion()
        {
            var read = new ColumnReadDto
            {
                Id = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                LaneId = Guid.NewGuid(),
                Name = "Done",
                Order = 3,
                RowVersion = [9, 9]
            };

            var del = read.ToDeleteDto();
            del.Id.Should().Be(read.Id);
            del.RowVersion.Should().Equal(read.RowVersion);
        }

        [Fact]
        public void ToRenameDto_From_ReadDto_Copies_Fields()
        {
            var read = new ColumnReadDto
            {
                Id = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                LaneId = Guid.NewGuid(),
                Name = "Todo",
                Order = 0,
                RowVersion = [5]
            };

            var rename = read.ToRenameDto();
            rename.Id.Should().Be(read.Id);
            rename.Name.Should().Be(read.Name);
            rename.RowVersion.Should().Equal(read.RowVersion);
        }
    }
}
