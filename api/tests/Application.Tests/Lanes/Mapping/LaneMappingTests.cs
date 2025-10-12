using Application.Lanes.DTOs;
using Application.Lanes.Mapping;
using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;

namespace Application.Tests.Lanes.Mapping
{
    public sealed class LaneMappingTests
    {
        [Fact]
        public void ToReadDto_Maps_All()
        {
            var e = Lane.Create(Guid.NewGuid(), LaneName.Create("Backlog"), 0);
            e.GetType().GetProperty("Id")!.SetValue(e, Guid.NewGuid());
            e.GetType().GetProperty("RowVersion")!.SetValue(e, new byte[] { 1 });

            var dto = e.ToReadDto();
            dto.Id.Should().Be(e.Id);
            dto.ProjectId.Should().Be(e.ProjectId);
            dto.Name.Should().Be(e.Name.Value);
            dto.Order.Should().Be(e.Order);
            dto.RowVersion.Should().Equal(e.RowVersion);
        }

        [Fact]
        public void CreateDto_To_Entity_Works()
        {
            var c = new LaneCreateDto { ProjectId = Guid.NewGuid(), Name = "Doing", Order = 1 };
            var e = c.ToEntity();
            e.ProjectId.Should().Be(c.ProjectId);
            e.Name.Value.Should().Be(c.Name);
            e.Order.Should().Be(c.Order);
        }
    }
}
