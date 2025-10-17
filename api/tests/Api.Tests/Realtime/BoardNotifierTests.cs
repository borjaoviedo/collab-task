using Api.Realtime;
using Application.TaskItems.Realtime;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace Api.Tests.Realtime
{
    public class BoardNotifier_Tests
    {
        [Fact]
        public async Task NotifyAsync_sends_to_group_with_expected_method_and_payload()
        {
            var hubClients = new Mock<IHubClients>();
            var groupClient = new Mock<IClientProxy>();
            var hubContext = new Mock<IHubContext<BoardHub>>();

            var projectId = Guid.NewGuid();
            var payload = new TaskItemCreatedPayload(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Title", "Desc", 1m);
            var evt = new TaskItemCreatedEvent(projectId, payload);

            hubClients.Setup(c => c.Group($"project:{projectId}")).Returns(groupClient.Object);
            hubContext.SetupGet(h => h.Clients).Returns(hubClients.Object);

            object[]? captured = null;
            groupClient
                .Setup(g => g.SendCoreAsync("board:event", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Callback<string, object[], CancellationToken>((_, args, __) => captured = args)
                .Returns(Task.CompletedTask);

            var notifier = new BoardNotifier(hubContext.Object);

            await notifier.NotifyAsync(projectId, evt, CancellationToken.None);

            Assert.NotNull(captured);
            Assert.Single(captured!);
            var o = captured![0];
            var t = o.GetType();
            Assert.Equal("task.created", t.GetProperty("type")!.GetValue(o));
            Assert.Equal(projectId, t.GetProperty("projectId")!.GetValue(o));
            Assert.Equal(payload, t.GetProperty("payload")!.GetValue(o));

            groupClient.Verify(g => g.SendCoreAsync("board:event", It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
