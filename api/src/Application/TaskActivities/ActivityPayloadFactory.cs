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

        public static ActivityPayload TaskMoved(Guid fromColumnId, Guid toColumnId) =>
            ActivityPayload.Create($$"""{"fromColumnId":"{{fromColumnId}}","toColumnId":"{{toColumnId}}"}""");

        public static ActivityPayload OwnerChanged(Guid? oldOwnerId, Guid? newOwnerId) =>
            ActivityPayload.Create($$"""{"oldOwnerId":{{ToJsonOrNull(oldOwnerId)}},"newOwnerId":{{ToJsonOrNull(newOwnerId)}}}""");

        public static ActivityPayload CoOwnerChanged(Guid userId, string change) =>
            ActivityPayload.Create($$"""{"userId":"{{userId}}","change":"{{change}}"}""");

        public static ActivityPayload NoteAdded(Guid noteId) =>
            ActivityPayload.Create($$"""{"noteId":"{{noteId}}"}""");

        public static ActivityPayload NoteEdited(Guid noteId) =>
            ActivityPayload.Create($$"""{"noteId":"{{noteId}}"}""");

        public static ActivityPayload NoteRemoved(Guid noteId) =>
            ActivityPayload.Create($$"""{"noteId":"{{noteId}}"}""");

        private static string JsonEscape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        private static string ToJsonOrNull(string? s) => s is null ? "null" : $"\"{JsonEscape(s)}\"";
        private static string ToJsonOrNull(Guid? g) => g is null ? "null" : $"\"{g}\"";
    }
}
