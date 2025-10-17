using Api.Realtime;
using Application.TaskAssignments.Realtime;
using Application.TaskItems.Realtime;
using Application.TaskNotes.Realtime;
using Domain.Enums;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace Api.Tests.Realtime
{
    public class BoardNotifierTests
    {
        [Fact]
        public async Task NotifyAsync_sends_to_group_with_expected_method_and_payload()
        {
            var (hubContext, groupClient, capture, projectId) = CreateHubContextWithCapture();

            var payload = new TaskItemCreatedPayload(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Title", "Desc", 1m);
            var evt = new TaskItemCreatedEvent(projectId, payload);

            var notifier = new BoardNotifier(hubContext.Object);

            await notifier.NotifyAsync(projectId, evt, CancellationToken.None);

            AssertCaptured(capture, "task.created", projectId, payload);
            groupClient.Verify(g => g.SendCoreAsync("board:event", It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task NotifyAsync_sends_task_updated_event()
        {
            var (hubContext, groupClient, capture, projectId) = CreateHubContextWithCapture();

            var payload = new TaskItemUpdatedPayload(Guid.NewGuid(), "New", "NewDesc", DateTimeOffset.UtcNow);
            var evt = new TaskItemUpdatedEvent(projectId, payload);

            var notifier = new BoardNotifier(hubContext.Object);
            await notifier.NotifyAsync(projectId, evt, CancellationToken.None);

            AssertCaptured(capture, "task.updated", projectId, payload);
            groupClient.Verify(g => g.SendCoreAsync("board:event", It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task NotifyAsync_sends_task_moved_event()
        {
            var (hubContext, groupClient, capture, projectId) = CreateHubContextWithCapture();

            var payload = new TaskItemMovedPayload(
                Guid.NewGuid(),
                FromLaneId: Guid.NewGuid(),
                FromColumnId: Guid.NewGuid(),
                ToLaneId: Guid.NewGuid(),
                ToColumnId: Guid.NewGuid(),
                SortKey: 5.25m);

            var evt = new TaskItemMovedEvent(projectId, payload);

            var notifier = new BoardNotifier(hubContext.Object);
            await notifier.NotifyAsync(projectId, evt, CancellationToken.None);

            AssertCaptured(capture, "task.moved", projectId, payload);
            groupClient.Verify(g => g.SendCoreAsync("board:event", It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task NotifyAsync_sends_task_deleted_event()
        {
            var (hubContext, groupClient, capture, projectId) = CreateHubContextWithCapture();

            var payload = new TaskItemDeletedPayload(Guid.NewGuid());
            var evt = new TaskItemDeletedEvent(projectId, payload);

            var notifier = new BoardNotifier(hubContext.Object);
            await notifier.NotifyAsync(projectId, evt, CancellationToken.None);

            AssertCaptured(capture, "task.deleted", projectId, payload);
            groupClient.Verify(g => g.SendCoreAsync("board:event", It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task NotifyAsync_sends_taskassignment_created_event()
        {
            var (hubContext, groupClient, capture, projectId) = CreateHubContextWithCapture();

            var payload = new TaskAssignmentCreatedPayload(
                TaskId: Guid.NewGuid(),
                UserId: Guid.NewGuid(),
                Role: TaskRole.CoOwner);

            var evt = new TaskAssignmentCreatedEvent(projectId, payload);
            var notifier = new BoardNotifier(hubContext.Object);

            await notifier.NotifyAsync(projectId, evt, CancellationToken.None);

            AssertCaptured(capture, "assignment.created", projectId, payload);
            groupClient.Verify(g => g.SendCoreAsync("board:event", It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task NotifyAsync_sends_taskassignment_updated_event()
        {
            var (hubContext, groupClient, capture, projectId) = CreateHubContextWithCapture();

            var payload = new TaskAssignmentUpdatedPayload(
                TaskId: Guid.NewGuid(),
                UserId: Guid.NewGuid(),
                NewRole: TaskRole.Owner);

            var evt = new TaskAssignmentUpdatedEvent(projectId, payload);
            var notifier = new BoardNotifier(hubContext.Object);

            await notifier.NotifyAsync(projectId, evt, CancellationToken.None);

            AssertCaptured(capture, "assignment.updated", projectId, payload);
            groupClient.Verify(g => g.SendCoreAsync("board:event", It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task NotifyAsync_sends_taskassignment_removed_event()
        {
            var (hubContext, groupClient, capture, projectId) = CreateHubContextWithCapture();

            var payload = new TaskAssignmentRemovedPayload(
                TaskId: Guid.NewGuid(),
                UserId: Guid.NewGuid());

            var evt = new TaskAssignmentRemovedEvent(projectId, payload);
            var notifier = new BoardNotifier(hubContext.Object);

            await notifier.NotifyAsync(projectId, evt, CancellationToken.None);

            AssertCaptured(capture, "assignment.removed", projectId, payload);
            groupClient.Verify(g => g.SendCoreAsync("board:event", It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task NotifyAsync_sends_tasknote_created_event()
        {
            var (hubContext, groupClient, capture, projectId) = CreateHubContextWithCapture();

            var payload = new TaskNoteCreatedPayload(
                NoteId: Guid.NewGuid(),
                TaskId: Guid.NewGuid(),
                Content: "note text");

            var evt = new TaskNoteCreatedEvent(projectId, payload);
            var notifier = new BoardNotifier(hubContext.Object);

            await notifier.NotifyAsync(projectId, evt, CancellationToken.None);

            AssertCaptured(capture, "note.created", projectId, payload);
            groupClient.Verify(g => g.SendCoreAsync("board:event", It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task NotifyAsync_sends_note_updated_event()
        {
            var (hubContext, groupClient, capture, projectId) = CreateHubContextWithCapture();

            var payload = new TaskNoteUpdatedPayload(Guid.NewGuid(), "new");
            var evt = new TaskNoteUpdatedEvent(projectId, payload);
            var notifier = new BoardNotifier(hubContext.Object);

            await notifier.NotifyAsync(projectId, evt, CancellationToken.None);

            AssertCaptured(capture, "note.updated", projectId, payload);
            groupClient.Verify(g => g.SendCoreAsync("board:event", It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task NotifyAsync_sends_note_deleted_event()
        {
            var (hubContext, groupClient, capture, projectId) = CreateHubContextWithCapture();

            var payload = new TaskNoteDeletedPayload(Guid.NewGuid());
            var evt = new TaskNoteDeletedEvent(projectId, payload);
            var notifier = new BoardNotifier(hubContext.Object);

            await notifier.NotifyAsync(projectId, evt, CancellationToken.None);

            AssertCaptured(capture, "note.deleted", projectId, payload);
            groupClient.Verify(g => g.SendCoreAsync("board:event", It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        // ---------- helpers ----------

        private static (Mock<IHubContext<BoardHub>> hubContext, Mock<IClientProxy> groupClient, Func<object[]?> capture, Guid projectId)
            CreateHubContextWithCapture()
        {
            var hubClients = new Mock<IHubClients>();
            var groupClient = new Mock<IClientProxy>();
            var hubContext = new Mock<IHubContext<BoardHub>>();

            var projectId = Guid.NewGuid();

            object[]? captured = null;
            groupClient
                .Setup(g => g.SendCoreAsync("board:event", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Callback<string, object[], CancellationToken>((_, args, __) => captured = args)
                .Returns(Task.CompletedTask);

            hubClients.Setup(c => c.Group($"project:{projectId}")).Returns(groupClient.Object);
            hubContext.SetupGet(h => h.Clients).Returns(hubClients.Object);

            return (hubContext, groupClient, () => captured, projectId);
        }

        private static void AssertCaptured(Func<object[]?> capture, string expectedType, Guid expectedProjectId, object expectedPayload)
        {
            var args = capture();
            Assert.NotNull(args);
            Assert.Single(args!);
            var evtObj = args![0];
            var t = evtObj.GetType();

            Assert.Equal(expectedType, t.GetProperty("type", System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!.GetValue(evtObj));
            Assert.Equal(expectedProjectId, t.GetProperty("projectId", System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!.GetValue(evtObj));
            Assert.Equal(expectedPayload, t.GetProperty("payload", System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!.GetValue(evtObj));
        }
    }
}
