using System.Text.Json.Serialization;

namespace Domain.Enums
{
    /// <summary>
    /// Specifies the types of activity events that can occur on a task.
    /// </summary>
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
