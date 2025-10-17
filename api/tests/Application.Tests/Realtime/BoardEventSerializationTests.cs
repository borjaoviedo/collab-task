using Application.TaskAssignments.Realtime;
using Application.TaskItems.Realtime;
using Application.TaskNotes.Realtime;
using Domain.Enums;
using System.Text.Json;

namespace Application.Tests.Realtime
{
    public class BoardEvent_Serialization_Tests
    {
        [Fact]
        public void TaskCreatedEvent_serializes_flat_payload()
        {
            var projectId = Guid.NewGuid();
            var payload = new TaskItemCreatedPayload(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Title", "Description", 1m);
            var evt = new TaskItemCreatedEvent(projectId, payload);

            var json = JsonSerializer.Serialize(evt, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            Assert.Contains("\"type\":\"task.created\"", json);
            Assert.Contains("\"payload\"", json);
            Assert.Contains("\"taskId\"", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"Task\":{", json, StringComparison.Ordinal);
        }

        [Fact]
        public void TaskNoteCreatedEvent_serializes_flat_payload()
        {
            var projectId = Guid.NewGuid();
            var payload = new TaskNoteCreatedPayload(
                TaskId: Guid.NewGuid(),
                NoteId: Guid.NewGuid(),
                Content: "Note content",
                AuthorId: Guid.NewGuid(),
                CreatedAt: DateTimeOffset.UtcNow);
            var evt = new TaskNoteCreatedEvent(projectId, payload);

            var json = JsonSerializer.Serialize(evt, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            Assert.Contains("\"type\":\"tasknote.created\"", json);
            Assert.Contains("\"payload\"", json);
            Assert.Contains("\"noteId\"", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"TaskNote\":{", json, StringComparison.Ordinal);
        }

        [Fact]
        public void TaskAssignmentCreatedEvent_serializes_flat_payload()
        {
            var projectId = Guid.NewGuid();
            var payload = new TaskAssignmentCreatedPayload(
                TaskId: Guid.NewGuid(),
                UserId: Guid.NewGuid(),
                Role: TaskRole.Owner);
            var evt = new TaskAssignmentCreatedEvent(projectId, payload);

            var json = JsonSerializer.Serialize(evt, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            Assert.Contains("\"type\":\"assignment.created\"", json);
            Assert.Contains("\"payload\"", json);
            Assert.Contains("\"userId\"", json, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("\"role\"", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"TaskAssignment\":{", json, StringComparison.Ordinal);
        }
    }
}
