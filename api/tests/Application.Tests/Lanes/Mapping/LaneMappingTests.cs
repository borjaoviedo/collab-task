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
            var entity = Lane.Create(Guid.NewGuid(), LaneName.Create("Backlog"), 0);
            entity.GetType().GetProperty("Id")!.SetValue(entity, Guid.NewGuid());
            entity.GetType().GetProperty("RowVersion")!.SetValue(entity, new byte[] { 1 });

            var dto = entity.ToReadDto();
            dto.Id.Should().Be(entity.Id);
            dto.ProjectId.Should().Be(entity.ProjectId);
            dto.Name.Should().Be(entity.Name.Value);
            dto.Order.Should().Be(entity.Order);
            dto.RowVersion.Should().Equal(entity.RowVersion);
        }
    }
}
