using System.Text.Json.Serialization;

namespace Domain.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TaskActivityType
    {
        TaskCreated,
        TaskEdited,
        TaskMoved,
        AssignmentCreated,
        AssignmentRoleChanged,
        AssignmentRemoved,
        NoteAdded,
        NoteEdited,
        NoteRemoved
    }
}
