using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;
using TestHelpers.Common.Testing;

namespace Domain.Tests.Entities
{
    [UnitTest]
    public sealed class ColumnTests
    {
        private static readonly Guid _defaultProjectId = Guid.NewGuid();
        private static readonly Guid _defaultLaneId = Guid.NewGuid();
        private static readonly ColumnName _defaultColumnName = ColumnName.Create("column");
        private static readonly int _defaultColumnOrder = 0;
        private readonly Column _defaultColumn = Column.Create(
            _defaultProjectId,
            _defaultLaneId,
            _defaultColumnName,
            _defaultColumnOrder);

        [Fact]
        public void Set_All_Core_Properties_Assigns_Correctly()
        {
            var order = 3;
            var column = Column.Create(
            _defaultProjectId,
            _defaultLaneId,
            _defaultColumnName,
            order);

            column.ProjectId.Should().Be(_defaultProjectId);
            column.LaneId.Should().Be(_defaultLaneId);
            column.Name.Should().Be(_defaultColumnName);
            column.Order.Should().Be(order);
        }

        [Fact]
        public void Column_Id_Is_Initialized()
        {
            var column = _defaultColumn;

            column.Id.Should().NotBeEmpty();
            column.Id.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public void Column_Is_Initialized_When_Null_Order()
        {
            var column = Column.Create(
                _defaultProjectId,
                _defaultLaneId,
                _defaultColumnName,
                order: null);

            column.Name.Value.Should().Be(_defaultColumnName.Value);
            column.Order.Should().Be(_defaultColumnOrder);
        }

        [Fact]
        public void Column_Is_Initialized_With_Order_0_When_Negative_Order()
        {
            var column = Column.Create(
                _defaultProjectId,
                _defaultLaneId,
                _defaultColumnName,
                order: -1);

            column.Order.Should().Be(_defaultColumnOrder);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("a")]
        public void Invalid_ColumnName_Throws(string input)
        {
            var act = () => Column.Create(
                _defaultProjectId,
                _defaultLaneId,
                name: ColumnName.Create(input),
                _defaultColumnOrder);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ProjectId_With_Guid_Empty_Throws()
        {
            var act = () => Column.Create(
                projectId: Guid.Empty,
                _defaultLaneId,
                _defaultColumnName,
                _defaultColumnOrder);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void LaneId_With_Guid_Empty_Throws()
        {
            var act = () => Column.Create(
                _defaultProjectId,
                laneId: Guid.Empty,
                _defaultColumnName,
                _defaultColumnOrder);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Rename_Changes_Name()
        {
            var column = _defaultColumn;
            var newColumnName = "new";

            column.Rename(ColumnName.Create(newColumnName));
            column.Name.Value.Should().Be(newColumnName);
        }

        [Fact]
        public void Rename_Same_Name_Does_Not_Change()
        {
            var column = _defaultColumn;

            column.Rename(_defaultColumnName);
            column.Name.Value.Should().Be(_defaultColumnName.Value);
        }

        [Fact]
        public void Reorder_Changes_Order()
        {
            var column = _defaultColumn;

            column.Reorder(2);
            column.Order.Should().Be(2);
        }

        [Fact]
        public void Reorder_Same_Order_Does_Not_Change()
        {
            var column = _defaultColumn;

            column.Reorder(_defaultColumnOrder);
            column.Order.Should().Be(_defaultColumnOrder);
        }

        [Fact]
        public void Reorder_Negative_Order_Throws()
        {
            var column = _defaultColumn;
            var act = () => column.Reorder(-1);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
