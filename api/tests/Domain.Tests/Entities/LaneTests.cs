using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;
using TestHelpers.Common.Testing;

namespace Domain.Tests.Entities
{
    [UnitTest]
    public sealed class LaneTests
    {
        private static readonly Guid _defaultProjectId = Guid.NewGuid();
        private static readonly LaneName _defaultLaneName = LaneName.Create("lane");
        private static readonly int _defaultLaneOrder = 0;
        private readonly Lane _defaultLane = Lane.Create(
            _defaultProjectId,
            _defaultLaneName,
            _defaultLaneOrder);

        [Fact]
        public void Set_All_Core_Properties_Assigns_Correctly()
        {
            var order = 2;
            var lane = Lane.Create(
                _defaultProjectId,
                _defaultLaneName,
                order);

            lane.ProjectId.Should().Be(_defaultProjectId);
            lane.Name.Should().Be(_defaultLaneName);
            lane.Order.Should().Be(order);
        }

        [Fact]
        public void Lane_Id_Is_Initialized()
        {
            var lane = _defaultLane;

            lane.Id.Should().NotBeEmpty();
            lane.Id.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public void Lane_Is_Initialized_When_Null_Order()
        {
            var lane = Lane.Create(
                _defaultProjectId,
                _defaultLaneName,
                order: null);

            lane.Name.Value.Should().Be(_defaultLaneName.Value); 
        }

        [Fact]
        public void Lane_Is_Initialized_With_Order_0_When_Negative_Order()
        {
            var lane = Lane.Create(
                _defaultProjectId,
                _defaultLaneName,
                order: -1);

            lane.Name.Value.Should().Be(_defaultLaneName.Value);
            lane.Order.Should().Be(0);
        }

        [Fact]
        public void ProjectId_With_Guid_Empty_Throws()
        {
            var act = () => Lane.Create(
                projectId: Guid.Empty,
                _defaultLaneName,
                _defaultLaneOrder);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Rename_Changes_Name()
        {
            var lane = _defaultLane;
            var newLaneName = "new";

            lane.Rename(LaneName.Create(newLaneName));
            lane.Name.Value.Should().Be(newLaneName);
        }

        [Fact]
        public void Rename_Same_Name_Does_Not_Change()
        {
            var lane = _defaultLane;

            lane.Rename(_defaultLaneName);
            lane.Name.Value.Should().Be(_defaultLaneName.Value);
        }

        [Fact]
        public void Reorder_Changes_Order()
        {
            var differentOrder = 2;
            var lane = _defaultLane;

            lane.Reorder(differentOrder);
            lane.Order.Should().Be(differentOrder);
        }

        [Fact]
        public void Reorder_Same_Order_Does_Not_Change()
        {
            var lane = _defaultLane;

            lane.Reorder(_defaultLaneOrder);
            lane.Order.Should().Be(_defaultLaneOrder);
        }

        [Fact]
        public void Reorder_Negative_Order_Throws()
        {
            var lane = _defaultLane;
            var act = () => lane.Reorder(-1);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
