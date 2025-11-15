using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.Tests.Entities
{
    public sealed class TaskItemTests
    {
        private readonly DateTimeOffset _utcNow = DateTimeOffset.UtcNow;
        private static readonly Guid _defaultColumnId = Guid.NewGuid();
        private static readonly Guid _defaultLaneId = Guid.NewGuid();
        private static readonly Guid _defaultProjectId = Guid.NewGuid();
        private static readonly TaskTitle _defaultTaskTitle = TaskTitle.Create("title");
        private static readonly TaskDescription _defaultTaskDescription = TaskDescription.Create("description");
        private static readonly Decimal _defaultSortKey = 0m;

        private readonly TaskItem _defaultTaskItem = TaskItem.Create(
            _defaultColumnId,
            _defaultLaneId,
            _defaultProjectId,
            _defaultTaskTitle,
            _defaultTaskDescription,
            dueDate: null,
            sortKey: null);

        [Fact]
        public void Set_All_Core_Properties_Assigns_Correctly()
        {
            var task = _defaultTaskItem;

            task.ProjectId.Should().Be(_defaultProjectId);
            task.LaneId.Should().Be(_defaultLaneId);
            task.ColumnId.Should().Be(_defaultColumnId);
            task.Title.Should().Be(_defaultTaskTitle);
            task.Description.Should().Be(_defaultTaskDescription);
        }

        [Fact]
        public void TaskItem_Id_Is_Initialized()
        {
            var task = _defaultTaskItem;

            task.Id.Should().NotBeEmpty();
            task.Id.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public void TaskItem_SortKey_Has_Default_Value_When_No_Value_Given()
        {
            var task = _defaultTaskItem;

            task.SortKey.Should().Be(_defaultSortKey);
        }

        [Fact]
        public void TaskItem_SortKey_Has_Different_Value_When_Value_Given()
        {
            var differentSortKey = 5m;
            var task = TaskItem.Create(
                _defaultColumnId,
                _defaultLaneId,
                _defaultProjectId,
                _defaultTaskTitle,
                _defaultTaskDescription,
                sortKey: differentSortKey);

            task.SortKey.Should().Be(differentSortKey);
        }

        [Fact]
        public void TaskItem_DueDate_Is_Null_When_No_Value_Given()
        {
            var task = _defaultTaskItem;

            task.DueDate.Should().Be(null);
        }

        [Fact]
        public void TaskItem_DueDate_Has_Value_When_Value_Given()
        {
            var dueDate = _utcNow.AddDays(3);
            var task = TaskItem.Create(
                _defaultColumnId,
                _defaultLaneId,
                _defaultProjectId,
                _defaultTaskTitle,
                _defaultTaskDescription,
                dueDate);

            task.DueDate.Should().Be(dueDate);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("a")]
        public void Invalid_TaskTitle_Throws(string input)
        {
            var act = () => TaskItem.Create(
                _defaultColumnId,
                _defaultLaneId,
                _defaultProjectId,
                title: TaskTitle.Create(input),
                _defaultTaskDescription);

            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("a")]
        public void Invalid_TaskDescription_Throws(string input)
        {
            var act = () => TaskItem.Create(
                _defaultColumnId,
                _defaultLaneId,
                _defaultProjectId,
                _defaultTaskTitle,
                description: TaskDescription.Create(input));

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Past_DueDate_Throws()
        {
            var act = () => TaskItem.Create(
                _defaultColumnId,
                _defaultLaneId,
                _defaultProjectId,
                _defaultTaskTitle,
                _defaultTaskDescription,
                dueDate: _utcNow.AddDays(-2));

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ColumnId_With_Guid_Empty_Throws()
        {
            var act = () => TaskItem.Create(
                columnId : Guid.Empty,
                _defaultLaneId,
                _defaultProjectId,
                _defaultTaskTitle,
                _defaultTaskDescription);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void LaneId_With_Guid_Empty_Throws()
        {
            var act = () => TaskItem.Create(
                _defaultColumnId,
                laneId: Guid.Empty,
                _defaultProjectId,
                _defaultTaskTitle,
                _defaultTaskDescription);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ProjectId_With_Guid_Empty_Throws()
        {
            var act = () => TaskItem.Create(
                _defaultColumnId,
                _defaultLaneId,
                projectId: Guid.Empty,
                _defaultTaskTitle,
                _defaultTaskDescription);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Edit_Changes_Title()
        {
            var task = _defaultTaskItem;
            var newTaskTitle = "New Title";

            task.Edit(
                title: TaskTitle.Create(newTaskTitle),
                description: null,
                dueDate: null);

            task.Title.Value.Should().Be(newTaskTitle);
        }

        [Fact]
        public void Edit_Changes_Description()
        {
            var task = _defaultTaskItem;
            var newTaskDescription = "New Description";

            task.Edit(
                title: null,
                description: TaskDescription.Create(newTaskDescription),
                dueDate: null);

            task.Description.Value.Should().Be(newTaskDescription);
        }

        [Fact]
        public void Edit_Changes_DueDate()
        {
            var task = TaskItem.Create(
                _defaultColumnId,
                _defaultLaneId,
                _defaultProjectId,
                _defaultTaskTitle,
                _defaultTaskDescription,
                dueDate: DateTimeOffset.UtcNow.AddDays(10));

            task.Edit(title: null, description: null, dueDate: null);

            task.DueDate.Should().BeNull();

            var newDueDate = _utcNow.AddDays(5);
            task.Edit(title: null, description: null, newDueDate);

            task.DueDate.Should().Be(newDueDate);
        }

        [Fact]
        public void Edit_With_Same_Values_Does_Not_Change_Entity_Values()
        {
            var dueDate = _utcNow.AddDays(2);
            var task = TaskItem.Create(
                _defaultColumnId,
                _defaultLaneId,
                _defaultProjectId,
                _defaultTaskTitle,
                _defaultTaskDescription,
                dueDate);

            task.Edit(_defaultTaskTitle, _defaultTaskDescription, dueDate);

            task.Title.Should().Be(_defaultTaskTitle);
            task.Description.Should().Be(_defaultTaskDescription);
            task.DueDate.Should().Be(dueDate); 
        }

        [Fact]
        public void Edit_With_Invalid_Values_Does_Not_Change_Entity_Values()
        {
            var dueDate = _utcNow.AddDays(20);
            var task = TaskItem.Create(
                _defaultColumnId,
                _defaultLaneId,
                _defaultProjectId,
                _defaultTaskTitle,
                _defaultTaskDescription,
                dueDate);

            var act = () => task.Edit(title: TaskTitle.Create("t"), description: null, dueDate: null);
            act.Should().Throw<ArgumentException>();

            act = () => task.Edit(title: null, description: TaskDescription.Create("d"), dueDate: null);
            act.Should().Throw<ArgumentException>();

            act = () => task.Edit(title: null, description: null, dueDate: _utcNow.AddSeconds(-1));
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Move_Changes_LaneId_ColumnId_SortKey()
        {
            var task = _defaultTaskItem;
            var newLaneId = Guid.NewGuid();
            var newColumnId = Guid.NewGuid();
            var newSortKey = 5m;

            task.Move(newLaneId, newColumnId, newSortKey);

            task.ProjectId.Should().Be(_defaultProjectId);
            task.LaneId.Should().Be(newLaneId);
            task.ColumnId.Should().Be(newColumnId);
            task.SortKey.Should().Be(newSortKey);
        }

        [Fact]
        public void Move_With_Guid_Empty_LaneId_Throws()
        {
            var task = _defaultTaskItem;
            var newLaneId = Guid.Empty;
            var newColumnId = Guid.NewGuid();
            var newSortKey = 5m;

            var act = () => task.Move(
                newLaneId,
                newColumnId,
                newSortKey);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Move_With_Guid_Empty_ColumnId_Throws()
        {
            var task = _defaultTaskItem;
            var newLaneId = Guid.NewGuid();
            var newColumnId = Guid.Empty;
            var newSortKey = 5m;

            var act = () => task.Move(
                newLaneId,
                newColumnId,
                newSortKey);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Move_With_Same_Values_Does_Not_Change_Entity_Values()
        {
            var task = _defaultTaskItem;

            task.Move(_defaultLaneId, _defaultColumnId, _defaultSortKey);

            task.ProjectId.Should().Be(_defaultProjectId);
            task.LaneId.Should().Be(_defaultLaneId);
            task.ColumnId.Should().Be(_defaultColumnId);
            task.SortKey.Should().Be(_defaultSortKey);
        }
    }
}
