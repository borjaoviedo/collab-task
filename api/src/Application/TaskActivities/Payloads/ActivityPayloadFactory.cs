using Domain.Enums;
using Domain.ValueObjects;

namespace Application.TaskActivities.Payloads
{
    /// <summary>
    /// Factory for constructing structured <see cref="ActivityPayload"/> objects used to record task and assignment events.
    /// </summary>
    public static class ActivityPayloadFactory
    {
        /// <summary>Creates a payload for a newly created task.</summary>
        public static ActivityPayload TaskCreated(string title) =>
            ActivityPayload.Create($$"""
            {
                "title":"{{JsonEscape(title)}}"
            }
            """);

        /// <summary>Creates a payload describing a task edit operation, including old and new title and description values.</summary>
        public static ActivityPayload TaskEdited(
            string? oldTitle,
            string? newTitle,
            string? oldDesc,
            string? newDesc) =>
            ActivityPayload.Create($$"""
            {
                "old":
                {
                    "title":{{ToJsonOrNull(oldTitle)}},
                    "desc":{{ToJsonOrNull(oldDesc)}}
                },
                "new":
                {
                    "title":{{ToJsonOrNull(newTitle)}},
                    "desc":{{ToJsonOrNull(newDesc)}}
                }
            }
            """);

        /// <summary>Creates a payload describing a task move between lanes and columns.</summary>
        public static ActivityPayload TaskMoved(
            Guid fromLaneId,
            Guid fromColumnId,
            Guid toLaneId,
            Guid toColumnId) =>
            ActivityPayload.Create($$"""
            {
                "from":
                {
                    "laneId": "{{fromLaneId}}",
                    "columnId": "{{fromColumnId}}"
                },
                "to":
                {
                    "laneId": "{{toLaneId}}",
                    "columnId": "{{toColumnId}}"
                }
            }
            """);

        /// <summary>Creates a payload describing the creation of a task assignment.</summary>
        public static ActivityPayload AssignmentCreated(Guid userId, TaskRole role) =>
            ActivityPayload.Create($$"""
            {
                "userId":"{{userId}}",
                "role":"{{role}}"
            }
            """);

        /// <summary>Creates a payload describing a task assignment role change.</summary>
        public static ActivityPayload AssignmentRoleChanged(
            Guid userId,
            TaskRole oldRole,
            TaskRole newRole) =>
            ActivityPayload.Create($$"""
            {
                "userId":"{{userId}}",
                "oldRole":"{{oldRole}}",
                "newRole":"{{newRole}}"
            }
            """);

        /// <summary>Creates a payload describing the removal of a task assignment.</summary>
        public static ActivityPayload AssignmentRemoved(Guid userId) =>
            ActivityPayload.Create($$"""
            {
                "userId":"{{userId}}"
            }
            """);

        /// <summary>Creates a payload describing a newly added task note.</summary>
        public static ActivityPayload NoteAdded(Guid noteId) =>
            ActivityPayload.Create($$"""
            {
                "noteId":"{{noteId}}"
            }
            """);

        /// <summary>Creates a payload describing an edited task note.</summary>
        public static ActivityPayload NoteEdited(Guid noteId) =>
            ActivityPayload.Create($$"""
            {
                "noteId":"{{noteId}}"
            }
            """);

        /// <summary>Creates a payload describing a removed task note.</summary>
        public static ActivityPayload NoteRemoved(Guid noteId) =>
            ActivityPayload.Create($$"""
            {
                "noteId":"{{noteId}}"
            }
            """);

        /// <summary>Escapes special characters for inclusion in JSON string literals.</summary>
        private static string JsonEscape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

        /// <summary>Converts a nullable string to a JSON literal or <c>null</c> token.</summary>
        private static string ToJsonOrNull(string? s) => s is null ? "null" : $"\"{JsonEscape(s)}\"";
    }
}
