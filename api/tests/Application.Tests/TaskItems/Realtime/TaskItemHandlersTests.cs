using Application.Realtime;
using Application.TaskItems.Realtime;
using Moq;

namespace Application.Tests.TaskItems.Realtime
{
    public class TaskItemHandlersTests
    {
        [Fact]
        public async Task CreatedHandler_calls_notifier_with_TaskCreatedEvent()
        {
            // Arrange
            var notifier = new Mock<IBoardNotifier>();
            var handler = new TaskItemChangedHandler(notifier.Object);
            var projectId = Guid.NewGuid();
            var payload = new TaskItemCreatedPayload(
                TaskId: Guid.NewGuid(),
                ColumnId: Guid.NewGuid(),
                LaneId: Guid.NewGuid(),
                Title: "Title",
                Description: "Desc",
                SortKey: 1m);

            // Act
            await handler.Handle(new TaskItemCreated(projectId, payload), CancellationToken.None);

            // Assert
            notifier.Verify(n => n.NotifyAsync(
                projectId,
                It.Is<BoardEvent<TaskItemCreatedPayload>>(e =>
                    e.Type == "task.created" &&
                    e.ProjectId == projectId &&
                    e.Payload == payload),
                It.IsAny<CancellationToken>()),
            Times.Once);
        }

        [Fact]
        public async Task UpdatedHandler_calls_notifier_with_TaskUpdatedEvent()
        {
            var notifier = new Mock<IBoardNotifier>();
            var handler = new TaskItemChangedHandler(notifier.Object);
            var projectId = Guid.NewGuid();
            var payload = new TaskItemUpdatedPayload(
                TaskId: Guid.NewGuid(),
                NewTitle: "New",
                NewDescription: "NewDesc",
                NewDueDate: DateTimeOffset.UtcNow);

            await handler.Handle(new TaskItemUpdated(projectId, payload), CancellationToken.None);

            notifier.Verify(n => n.NotifyAsync(
                projectId,
                It.Is<BoardEvent<TaskItemUpdatedPayload>>(e =>
                    e.Type == "task.updated" &&
                    e.ProjectId == projectId &&
                    e.Payload == payload),
                It.IsAny<CancellationToken>()),
            Times.Once);
        }

        [Fact]
        public async Task MovedHandler_calls_notifier_with_TaskMovedEvent()
        {
            var notifier = new Mock<IBoardNotifier>();
            var handler = new TaskItemChangedHandler(notifier.Object);
            var projectId = Guid.NewGuid();
            var payload = new TaskItemMovedPayload(
                TaskId: Guid.NewGuid(),
                FromLaneId: Guid.NewGuid(),
                FromColumnId: Guid.NewGuid(),
                ToLaneId: Guid.NewGuid(),
                ToColumnId: Guid.NewGuid(),
                SortKey: 2.5m);

            await handler.Handle(new TaskItemMoved(projectId, payload), CancellationToken.None);

            notifier.Verify(n => n.NotifyAsync(
                projectId,
                It.Is<BoardEvent<TaskItemMovedPayload>>(e =>
                    e.Type == "task.moved" &&
                    e.ProjectId == projectId &&
                    e.Payload == payload),
                It.IsAny<CancellationToken>()),
            Times.Once);
        }
    }
}
