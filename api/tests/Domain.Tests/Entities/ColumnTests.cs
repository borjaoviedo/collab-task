using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.Tests.Entities
{
    public sealed class ColumnTests
    {
        [Fact]
        public void Set_All_Core_Properties_Assigns_Correctly()
        {
            var projectId = Guid.NewGuid();
            var laneId = Guid.NewGuid();
            var name = ColumnName.Create("Demo column");
            var order = 3;

            var c = Column.Create(projectId, laneId, name, order);

            c.ProjectId.Should().Be(projectId);
            c.LaneId.Should().Be(laneId);
            c.Name.Should().Be(name);
            c.Order.Should().Be(order);
        }

        [Fact]
        public void Column_Id_Is_Initialized()
        {
            var c = Column.Create(Guid.NewGuid(), Guid.NewGuid(), ColumnName.Create("column"), 1);

            c.Id.Should().NotBeEmpty();
            c.Id.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public void Column_Is_Initialized_When_Null_Order()
        {
            var c = Column.Create(Guid.NewGuid(), Guid.NewGuid(), ColumnName.Create("column"), null);

            c.Name.Value.Should().Be("column");
        }

        [Fact]
        public void Column_Is_Initialized_With_Order_0_When_Negative_Order()
        {
            var c = Column.Create(Guid.NewGuid(), Guid.NewGuid(), ColumnName.Create("column"), -1);

            c.Name.Value.Should().Be("column");
            c.Order.Should().Be(0);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("a")]
        public void Invalid_ColumnName_Throws(string input)
        {
            Action act = () => Column.Create(Guid.NewGuid(), Guid.NewGuid(), ColumnName.Create(input), 1);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ProjectId_With_Guid_Empty_Throws()
        {
            var projectId = Guid.Empty;
            Action act = () => Column.Create(projectId, Guid.NewGuid(), ColumnName.Create("column"), 1);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void LaneId_With_Guid_Empty_Throws()
        {
            var laneId = Guid.Empty;
            Action act = () => Column.Create(Guid.NewGuid(), laneId, ColumnName.Create("column"), 1);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Rename_Changes_Name()
        {
            var c = Column.Create(Guid.NewGuid(), Guid.NewGuid(), ColumnName.Create("column"), 1);
            c.Rename(ColumnName.Create("New column"));
            c.Name.Value.Should().Be("New column");
        }

        [Fact]
        public void Rename_Same_Name_Does_Not_Change()
        {
            var c = Column.Create(Guid.NewGuid(), Guid.NewGuid(), ColumnName.Create("column"), 1);
            c.Rename(ColumnName.Create("column"));
            c.Name.Value.Should().Be("column");
        }

        [Fact]
        public void Reorder_Changes_Order()
        {
            var c = Column.Create(Guid.NewGuid(), Guid.NewGuid(), ColumnName.Create("column"), 1);
            c.Reorder(2);
            c.Order.Should().Be(2);
        }

        [Fact]
        public void Reorder_Same_Order_Does_Not_Change()
        {
            var c = Column.Create(Guid.NewGuid(), Guid.NewGuid(), ColumnName.Create("column"), 1);
            c.Reorder(1);
            c.Order.Should().Be(1);
        }

        [Fact]
        public void Reorder_Negative_Order_Throws()
        {
            var c = Column.Create(Guid.NewGuid(), Guid.NewGuid(), ColumnName.Create("column"), 1);
            Action act = () => c.Reorder(-1);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
