using Application.TaskItems.Realtime;
using System.Text.Json;

namespace Application.Tests.Realtime
{
    public class BoardEvent_Serialization_Tests
    {
        [Fact]
        public void TaskCreatedEvent_serializes_flat_payload()
        {
            var projectId = Guid.NewGuid();
            var payload = new TaskCreatedPayload(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Title", "Description", 1m);
            var evt = new TaskCreatedEvent(projectId, payload);

            var json = JsonSerializer.Serialize(evt, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            Assert.Contains("\"type\":\"task.created\"", json);
            Assert.Contains("\"payload\"", json);
            Assert.Contains("\"taskId\"", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"Task\":{", json, StringComparison.Ordinal);
        }
    }
}
