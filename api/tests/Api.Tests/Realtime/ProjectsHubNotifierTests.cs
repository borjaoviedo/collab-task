using Api.Realtime;
using Application.TaskAssignments.Realtime;
using Application.TaskItems.Realtime;
using Application.TaskNotes.Realtime;
using Domain.Enums;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace Api.Tests.Realtime
{
    public class ProjectsHubNotifierTests
    {
        [Fact]
        public async Task NotifyAsync_Sends_To_Correct_Project_Group_Only()
        {
            var hubClients = new Mock<IHubClients>();
            var hubContext = new Mock<IHubContext<ProjectsHub>>();
            var groupClient1 = new Mock<IClientProxy>();
            var groupClient2 = new Mock<IClientProxy>();
            var projectId1 = Guid.NewGuid();
            var projectId2 = Guid.NewGuid();

            hubClients.Setup(c => c.Group($"project:{projectId1}")).Returns(groupClient1.Object);
            hubClients.Setup(c => c.Group($"project:{projectId2}")).Returns(groupClient2.Object);
            hubContext.SetupGet(h => h.Clients).Returns(hubClients.Object);

            var notifier = new ProjectsHubNotifier(hubContext.Object);
            var payload = new TaskItemDeletedPayload(TaskId: Guid.NewGuid());
            var evt = new TaskItemDeletedEvent(projectId1, payload);

            await notifier.NotifyAsync(projectId1, evt);

            groupClient1.Verify(g => g.SendCoreAsync(
                "board:event",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
            groupClient2.Verify(g => g.SendCoreAsync(
                "board:event",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task NotifyAsync_Respects_CancellationToken()
        {
            var hubClients = new Mock<IHubClients>();
            var groupClient = new Mock<IClientProxy>();
            var hubContext = new Mock<IHubContext<ProjectsHub>>();
            var projectId = Guid.NewGuid();

            groupClient
                .Setup(g => g.SendCoreAsync(
                    "board:event",
                    It.IsAny<object[]>(),
                    It.IsAny<CancellationToken>()))
                .Callback<string, object[], CancellationToken>((_, __, token) =>
                {
                    if (token.IsCancellationRequested) throw new TaskCanceledException();
                })
                .Returns(Task.CompletedTask);

            hubClients.Setup(c => c.Group($"project:{projectId}")).Returns(groupClient.Object);
            hubContext.SetupGet(h => h.Clients).Returns(hubClients.Object);

            var notifier = new ProjectsHubNotifier(hubContext.Object);
            var payload = new TaskNoteDeletedPayload(TaskId: Guid.NewGuid(), NoteId: Guid.NewGuid());
            var evt = new TaskNoteDeletedEvent(projectId, payload);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAsync<TaskCanceledException>(()
                => notifier.NotifyAsync(projectId, evt, cts.Token));
        }

        [Fact]
        public async Task NotifyAsync_Sets_OccurredAt_To_Recent_Utc_Time()
        {
            var (hubContext, _, capture, projectId) = CreateHubContextWithCapture();
            var before = DateTimeOffset.UtcNow;
            var payload = new TaskItemCreatedPayload(
                TaskId: Guid.NewGuid(),
                ColumnId: Guid.NewGuid(),
                LaneId: Guid.NewGuid(),
                Title: "title",
                Description: "description",
                SortKey: 1m);
            var evt = new TaskItemCreatedEvent(projectId, payload);
            var notifier = new ProjectsHubNotifier(hubContext.Object);

            await notifier.NotifyAsync(projectId, evt);
            var after = DateTimeOffset.UtcNow;

            var args = capture();
            Assert.NotNull(args);
            var eventObject = args![0];
            var type = eventObject.GetType();
            var occurredAt = (DateTimeOffset)type.GetProperty(
                "occurredAt",
                System.Reflection.BindingFlags.IgnoreCase
                | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Instance)!.GetValue(eventObject)!;

            Assert.True(occurredAt >= before && occurredAt <= after);
        }

        [Fact]
        public async Task NotifyAsync_Sends_TaskItem_Created_Event()
        {
            var (hubContext, groupClient, capture, projectId) = CreateHubContextWithCapture();
            var payload = new TaskItemCreatedPayload(
                TaskId: Guid.NewGuid(),
                ColumnId: Guid.NewGuid(),
                LaneId: Guid.NewGuid(),
                Title: "Title",
                Description: "Desc",
                SortKey: 1m);
            var evt = new TaskItemCreatedEvent(projectId, payload);
            var notifier = new ProjectsHubNotifier(hubContext.Object);

            await notifier.NotifyAsync(projectId, evt);

            AssertCaptured(capture, evt.Type, projectId, payload);
            groupClient.Verify(g => g.SendCoreAsync(
                "board:event",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task NotifyAsync_Sends_TaskItem_Updated_Event()
        {
            var (hubContext, groupClient, capture, projectId) = CreateHubContextWithCapture();
            var payload = new TaskItemUpdatedPayload(
                TaskId: Guid.NewGuid(),
                NewTitle: "New",
                NewDescription: "NewDesc",
                NewDueDate: DateTimeOffset.UtcNow.AddDays(5));
            var evt = new TaskItemUpdatedEvent(projectId, payload);
            var notifier = new ProjectsHubNotifier(hubContext.Object);

            await notifier.NotifyAsync(projectId, evt);

            AssertCaptured(capture, evt.Type, projectId, payload);
            groupClient.Verify(g => g.SendCoreAsync(
                "board:event",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task NotifyAsync_Sends_TaskItem_Moved_Event()
        {
            var (hubContext, groupClient, capture, projectId) = CreateHubContextWithCapture();
            var payload = new TaskItemMovedPayload(
                TaskId: Guid.NewGuid(),
                FromLaneId: Guid.NewGuid(),
                FromColumnId: Guid.NewGuid(),
                ToLaneId: Guid.NewGuid(),
                ToColumnId: Guid.NewGuid(),
                SortKey: 5.25m);
            var evt = new TaskItemMovedEvent(projectId, payload);
            var notifier = new ProjectsHubNotifier(hubContext.Object);

            await notifier.NotifyAsync(projectId, evt);

            AssertCaptured(capture, evt.Type, projectId, payload);
            groupClient.Verify(g => g.SendCoreAsync(
                "board:event",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task NotifyAsync_Sends_TaskItem_Deleted_Event()
        {
            var (hubContext, groupClient, capture, projectId) = CreateHubContextWithCapture();
            var payload = new TaskItemDeletedPayload(TaskId: Guid.NewGuid());
            var evt = new TaskItemDeletedEvent(projectId, payload);
            var notifier = new ProjectsHubNotifier(hubContext.Object);

            await notifier.NotifyAsync(projectId, evt);

            AssertCaptured(capture, evt.Type, projectId, payload);
            groupClient.Verify(g => g.SendCoreAsync(
                "board:event",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task NotifyAsync_Sends_TaskAssignment_Created_Event()
        {
            var (hubContext, groupClient, capture, projectId) = CreateHubContextWithCapture();
            var payload = new TaskAssignmentCreatedPayload(
                TaskId: Guid.NewGuid(),
                UserId: Guid.NewGuid(),
                Role: TaskRole.CoOwner);
            var evt = new TaskAssignmentCreatedEvent(projectId, payload);
            var notifier = new ProjectsHubNotifier(hubContext.Object);

            await notifier.NotifyAsync(projectId, evt);

            AssertCaptured(capture, evt.Type, projectId, payload);
            groupClient.Verify(g => g.SendCoreAsync(
                "board:event",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task NotifyAsync_Sends_TaskAssignment_Updated_Event()
        {
            var (hubContext, groupClient, capture, projectId) = CreateHubContextWithCapture();
            var payload = new TaskAssignmentUpdatedPayload(
                TaskId: Guid.NewGuid(),
                UserId: Guid.NewGuid(),
                NewRole: TaskRole.Owner);
            var evt = new TaskAssignmentUpdatedEvent(projectId, payload);
            var notifier = new ProjectsHubNotifier(hubContext.Object);

            await notifier.NotifyAsync(projectId, evt);

            AssertCaptured(capture, evt.Type, projectId, payload);
            groupClient.Verify(g => g.SendCoreAsync(
                "board:event",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task NotifyAsync_Sends_TaskAssignment_Removed_Event()
        {
            var (hubContext, groupClient, capture, projectId) = CreateHubContextWithCapture();
            var payload = new TaskAssignmentRemovedPayload(
                TaskId: Guid.NewGuid(),
                UserId: Guid.NewGuid());
            var evt = new TaskAssignmentRemovedEvent(projectId, payload);
            var notifier = new ProjectsHubNotifier(hubContext.Object);

            await notifier.NotifyAsync(projectId, evt);

            AssertCaptured(capture, evt.Type, projectId, payload);
            groupClient.Verify(g => g.SendCoreAsync(
                "board:event",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task NotifyAsync_Sends_TaskNote_Created_Event()
        {
            var (hubContext, groupClient, capture, projectId) = CreateHubContextWithCapture();
            var payload = new TaskNoteCreatedPayload(
                NoteId: Guid.NewGuid(),
                TaskId: Guid.NewGuid(),
                Content: "note text");
            var evt = new TaskNoteCreatedEvent(projectId, payload);
            var notifier = new ProjectsHubNotifier(hubContext.Object);

            await notifier.NotifyAsync(projectId, evt);

            AssertCaptured(capture, evt.Type, projectId, payload);
            groupClient.Verify(g => g.SendCoreAsync(
                "board:event",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task NotifyAsync_Sends_TaskNote_Updated_Event()
        {
            var (hubContext, groupClient, capture, projectId) = CreateHubContextWithCapture();
            var payload = new TaskNoteUpdatedPayload(
                TaskId: Guid.NewGuid(),
                NoteId: Guid.NewGuid(),
                NewContent: "new");
            var evt = new TaskNoteUpdatedEvent(projectId, payload);
            var notifier = new ProjectsHubNotifier(hubContext.Object);

            await notifier.NotifyAsync(projectId, evt);

            AssertCaptured(capture, evt.Type, projectId, payload);
            groupClient.Verify(g => g.SendCoreAsync(
                "board:event",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task NotifyAsync_Sends_TaskNote_Deleted_Event()
        {
            var (hubContext, groupClient, capture, projectId) = CreateHubContextWithCapture();
            var payload = new TaskNoteDeletedPayload(TaskId: Guid.NewGuid(), NoteId: Guid.NewGuid());
            var evt = new TaskNoteDeletedEvent(projectId, payload);
            var notifier = new ProjectsHubNotifier(hubContext.Object);

            await notifier.NotifyAsync(projectId, evt);

            AssertCaptured(capture, evt.Type, projectId, payload);
            groupClient.Verify(g => g.SendCoreAsync(
                "board:event",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        // ---------- helpers ----------

        private static (
            Mock<IHubContext<ProjectsHub>> hubContext,
            Mock<IClientProxy> groupClient,
            Func<object[]?> capture,
            Guid projectId)
            CreateHubContextWithCapture()
        {
            var hubClients = new Mock<IHubClients>();
            var groupClient = new Mock<IClientProxy>();
            var hubContext = new Mock<IHubContext<ProjectsHub>>();

            var projectId = Guid.NewGuid();

            object[]? captured = null;
            groupClient
                .Setup(g => g.SendCoreAsync(
                    "board:event",
                    It.IsAny<object[]>(),
                    It.IsAny<CancellationToken>()))
                .Callback<string, object[], CancellationToken>((_, args, __) => captured = args)
                .Returns(Task.CompletedTask);

            hubClients.Setup(c => c.Group($"project:{projectId}")).Returns(groupClient.Object);
            hubContext.SetupGet(h => h.Clients).Returns(hubClients.Object);

            return (hubContext, groupClient, () => captured, projectId);
        }

        private static void AssertCaptured(
            Func<object[]?> capture,
            string expectedType,
            Guid expectedProjectId,
            object expectedPayload)
        {
            var args = capture();
            Assert.NotNull(args);
            Assert.Single(args!);
            var eventObject = args![0];
            var type = eventObject.GetType();

            Assert.Equal(
                expectedType,
                type.GetProperty(
                    "type",
                    System.Reflection.BindingFlags.IgnoreCase
                    | System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.Instance)!.GetValue(eventObject));
            Assert.Equal(
                expectedProjectId,
                type.GetProperty(
                    "projectId",
                    System.Reflection.BindingFlags.IgnoreCase
                    | System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.Instance)!.GetValue(eventObject));
            Assert.Equal(
                expectedPayload,
                type.GetProperty(
                    "payload",
                    System.Reflection.BindingFlags.IgnoreCase
                    | System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.Instance)!.GetValue(eventObject));
        }
    }
}
