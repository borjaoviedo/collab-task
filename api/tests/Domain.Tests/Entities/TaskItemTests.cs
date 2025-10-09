using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.Tests.Entities
{
    public sealed class TaskItemTests
    {
        [Fact]
        public void Set_All_Core_Properties_Assigns_Correctly()
        {
            var projectId = Guid.NewGuid();
            var laneId = Guid.NewGuid();
            var columnId = Guid.NewGuid();
            var title = TaskTitle.Create("Task Title");
            var description = TaskDescription.Create("Description here.");

            var t = TaskItem.Create(columnId, laneId, projectId, title, description);

            t.ProjectId.Should().Be(projectId);
            t.LaneId.Should().Be(laneId);
            t.ColumnId.Should().Be(columnId);
            t.Title.Should().Be(title);
            t.Description.Should().Be(description);
        }

        [Fact]
        public void TaskItem_Id_Is_Initialized()
        {
            var t = TaskItem.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TaskTitle.Create("Title"), TaskDescription.Create("Description"));

            t.Id.Should().NotBeEmpty();
            t.Id.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public void TaskItem_SortKey_Has_Default_Value_When_No_Value_Given()
        {
            var t = TaskItem.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TaskTitle.Create("Title"), TaskDescription.Create("Description"));

            t.SortKey.Should().Be(0m);
        }

        [Fact]
        public void TaskItem_SortKey_Has_Different_Value_When_Value_Given()
        {
            var t = TaskItem.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TaskTitle.Create("Title"), TaskDescription.Create("Description"), sortKey: 5m);

            t.SortKey.Should().Be(5m);
        }

        [Fact]
        public void TaskItem_DueDate_Is_Null_When_No_Value_Given()
        {
            var t = TaskItem.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TaskTitle.Create("Title"), TaskDescription.Create("Description"));

            t.DueDate.Should().Be(null);
        }

        [Fact]
        public void TaskItem_DueDate_Has_Value_When_Value_Given()
        {
            var dueDate = DateTimeOffset.UtcNow.AddDays(2);
            var t = TaskItem.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TaskTitle.Create("Title"), TaskDescription.Create("Description"), dueDate);

            t.DueDate.Should().Be(dueDate);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("a")]
        public void Invalid_TaskTitle_Throws(string input)
        {
            Action act = () => TaskItem.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TaskTitle.Create(input), TaskDescription.Create("Description"));
            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("a")]
        public void Invalid_TaskDescription_Throws(string input)
        {
            Action act = () => TaskItem.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TaskTitle.Create("Title"), TaskDescription.Create(input));
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Past_DueDate_Throws()
        {
            Action act = () => TaskItem.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TaskTitle.Create("Title"), TaskDescription.Create("Description"), DateTimeOffset.UtcNow.AddDays(-1));
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ColumnId_With_Guid_Empty_Throws()
        {
            var columnId = Guid.Empty;
            Action act = () => TaskItem.Create(columnId, Guid.NewGuid(), Guid.NewGuid(), TaskTitle.Create("Title"), TaskDescription.Create("Description"));
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void LaneId_With_Guid_Empty_Throws()
        {
            var laneId = Guid.Empty;
            Action act = () => TaskItem.Create(Guid.NewGuid(), laneId, Guid.NewGuid(), TaskTitle.Create("Title"), TaskDescription.Create("Description"));
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ProjectId_With_Guid_Empty_Throws()
        {
            var projectId = Guid.Empty;
            Action act = () => TaskItem.Create(Guid.NewGuid(), Guid.NewGuid(), projectId, TaskTitle.Create("Title"), TaskDescription.Create("Description"));
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Edit_Changes_Title()
        {
            var t = TaskItem.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TaskTitle.Create("Title"), TaskDescription.Create("Description"));
            t.Edit(TaskTitle.Create("New title"), null, null);
            t.Title.Value.Should().Be("New title");
        }

        [Fact]
        public void Edit_Changes_Description()
        {
            var t = TaskItem.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TaskTitle.Create("Title"), TaskDescription.Create("Description"));
            t.Edit(null, TaskDescription.Create("New description"), null);
            t.Description.Value.Should().Be("New description");
        }

        [Fact]
        public void Edit_Changes_DueDate()
        {
            var t = TaskItem.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TaskTitle.Create("Title"), TaskDescription.Create("Description"), dueDate: DateTimeOffset.UtcNow.AddDays(10));
            t.Edit(null, null, null);
            t.DueDate.Should().Be(null);

            var newDueDate = DateTimeOffset.UtcNow.AddDays(5);
            t.Edit(null, null, newDueDate);
            t.DueDate.Should().Be(newDueDate);
        }

        [Fact]
        public void Edit_With_Same_Values_Does_Not_Change_Entity_Values()
        {
            var title = TaskTitle.Create("Title");
            var description = TaskDescription.Create("Description");
            var dueDate = DateTimeOffset.UtcNow.AddDays(2);

            var t = TaskItem.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), title,description, dueDate);
            t.Edit(title, description, dueDate);

            t.Title.Should().Be(title);
            t.Description.Should().Be(description);
            t.DueDate.Should().Be(dueDate); 
        }

        [Fact]
        public void Edit_With_Invalid_Values_Does_Not_Change_Entity_Values()
        {
            var title = TaskTitle.Create("Title");
            var description = TaskDescription.Create("Description");
            var dueDate = DateTimeOffset.UtcNow.AddDays(10);
            var t = TaskItem.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), title, description, dueDate);

            Action act = () => t.Edit(TaskTitle.Create("t"), null, null);
            act.Should().Throw<ArgumentException>();

            act = () => t.Edit(null, TaskDescription.Create("d"), null);
            act.Should().Throw<ArgumentException>();

            act = () => t.Edit(null, null, DateTimeOffset.UtcNow.AddSeconds(-1));
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Move_Changes_LaneId_ColumnId_SortKey()
        {
            var t = TaskItem.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TaskTitle.Create("Title"), TaskDescription.Create("Description"), sortKey: 1m);
            var newLaneId = Guid.NewGuid();
            var newColumnId = Guid.NewGuid();
            var newSortKey = 5m;

            t.Move(newLaneId, newColumnId, newSortKey);
            t.LaneId.Should().Be(newLaneId);
            t.ColumnId.Should().Be(newColumnId);
            t.SortKey.Should().Be(newSortKey);
        }

        [Fact]
        public void Move_With_Guid_Empty_LaneId_Throws()
        {
            var t = TaskItem.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TaskTitle.Create("Title"), TaskDescription.Create("Description"), sortKey: 1m);
            var newLaneId = Guid.Empty;
            var newColumnId = Guid.NewGuid();
            var newSortKey = 5m;
            Action act = () => t.Move(newLaneId, newColumnId, newSortKey);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Move_With_Guid_Empty_ColumnId_Throws()
        {
            var t = TaskItem.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TaskTitle.Create("Title"), TaskDescription.Create("Description"), sortKey: 1m);
            var newLaneId = Guid.NewGuid();
            var newColumnId = Guid.Empty;
            var newSortKey = 5m;
            Action act = () => t.Move(newLaneId, newColumnId, newSortKey);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Move_With_Same_Values_Does_Not_Change_Entity_Values()
        {
            var laneId = Guid.NewGuid();
            var columnId = Guid.NewGuid();
            var sortKey = 1m;
            var t = TaskItem.Create(columnId, laneId, Guid.NewGuid(), TaskTitle.Create("Title"), TaskDescription.Create("Description"), sortKey: sortKey);
            t.Move(laneId, columnId, sortKey);
            t.LaneId.Should().Be(laneId);
            t.ColumnId.Should().Be(columnId);
            t.SortKey.Should().Be(sortKey);
        }
    }
}
