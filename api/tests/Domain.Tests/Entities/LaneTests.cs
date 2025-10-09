using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.Tests.Entities
{
    public sealed class LaneTests
    {
        [Fact]
        public void Set_All_Core_Properties_Assigns_Correctly()
        {
            var projectId = Guid.NewGuid();
            var name = LaneName.Create("Demo Line");
            var order = 0;

            var l = Lane.Create(projectId, name, order);

            l.ProjectId.Should().Be(projectId);
            l.Name.Should().Be(name);
            l.Order.Should().Be(order);
        }

        [Fact]
        public void Lane_Id_Is_Initialized()
        {
            var l = Lane.Create(Guid.NewGuid(), LaneName.Create("Line"), 1);

            l.Id.Should().NotBeEmpty();
            l.Id.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public void Lane_Is_Initialized_When_Null_Order()
        {
            var l = Lane.Create(Guid.NewGuid(), LaneName.Create("Line"), null);

            l.Name.Value.Should().Be("Line"); 
        }

        [Fact]
        public void Lane_Is_Initialized_With_Order_0_When_Negative_Order()
        {
            var l = Lane.Create(Guid.NewGuid(), LaneName.Create("Line"), -1);

            l.Name.Value.Should().Be("Line");
            l.Order.Should().Be(0);
        }

        [Fact]
        public void Rename_Changes_Name()
        {
            var l = Lane.Create(Guid.NewGuid(), LaneName.Create("Line"), 1);
            l.Rename(LaneName.Create("New Line"));
            l.Name.Value.Should().Be("New Line");
        }

        [Fact]
        public void Rename_Same_Name_Does_Not_Change()
        {
            var l = Lane.Create(Guid.NewGuid(), LaneName.Create("Line"), 1);
            l.Rename(LaneName.Create("Line"));
            l.Name.Value.Should().Be("Line");
        }

        [Fact]
        public void Reorder_Changes_Order()
        {
            var l = Lane.Create(Guid.NewGuid(), LaneName.Create("Line"), 1);
            l.Reorder(2);
            l.Order.Should().Be(2);
        }

        [Fact]
        public void Reorder_Same_Order_Does_Not_Change()
        {
            var l = Lane.Create(Guid.NewGuid(), LaneName.Create("Line"), 1);
            l.Reorder(1);
            l.Order.Should().Be(1);
        }

        [Fact]
        public void Reorder_Negative_Order_Throws()
        {
            var l = Lane.Create(Guid.NewGuid(), LaneName.Create("Line"), 1);
            Action act = () => l.Reorder(-1);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
