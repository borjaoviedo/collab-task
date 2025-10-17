using Domain.Enums;
using Domain.ValueObjects;

namespace Application.TaskActivities
{
    public static class ActivityPayloadFactory
    {
        public static ActivityPayload TaskCreated(string title) =>
            ActivityPayload.Create($$"""
        {"title":"{{JsonEscape(title)}}"}
        """);

        public static ActivityPayload TaskEdited(string? oldTitle, string? newTitle, string? oldDesc, string? newDesc) =>
            ActivityPayload.Create($$"""
        {"old":{"title":{{ToJsonOrNull(oldTitle)}},"desc":{{ToJsonOrNull(oldDesc)}}},
         "new":{"title":{{ToJsonOrNull(newTitle)}},"desc":{{ToJsonOrNull(newDesc)}}}
         }
        """);

        public static ActivityPayload TaskMoved(Guid fromLaneId, Guid fromColumnId, Guid toLaneId, Guid toColumnId) =>
            ActivityPayload.Create($$"""
        {
            "from": { "laneId": "{{fromLaneId}}", "columnId": "{{fromColumnId}}" },
            "to":   { "laneId": "{{toLaneId}}",   "columnId": "{{toColumnId}}" }
        }
        """);

        public static ActivityPayload AssignmentCreated(Guid userId, TaskRole role) =>
            ActivityPayload.Create($$"""{"userId":"{{userId}}","role":"{{role}}"}""");

        public static ActivityPayload AssignmentRoleChanged(Guid userId, TaskRole oldRole, TaskRole newRole) =>
    ActivityPayload.Create($$"""{"userId":"{{userId}}","oldRole":"{{oldRole}}","newRole":"{{newRole}}"}""");

        public static ActivityPayload AssignmentRemoved(Guid userId) =>
            ActivityPayload.Create($$"""{"userId":"{{userId}}"}""");

        public static ActivityPayload NoteAdded(Guid noteId) =>
            ActivityPayload.Create($$"""{"noteId":"{{noteId}}"}""");

        public static ActivityPayload NoteEdited(Guid noteId) =>
            ActivityPayload.Create($$"""{"noteId":"{{noteId}}"}""");

        public static ActivityPayload NoteRemoved(Guid noteId) =>
            ActivityPayload.Create($$"""{"noteId":"{{noteId}}"}""");

        private static string JsonEscape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        private static string ToJsonOrNull(string? s) => s is null ? "null" : $"\"{JsonEscape(s)}\"";
    }
}
