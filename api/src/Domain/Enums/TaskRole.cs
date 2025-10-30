using System.Text.Json.Serialization;

namespace Domain.Enums
{
    /// <summary>
    /// Defines the role of a user assigned to a task.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TaskRole
    {
        CoOwner,
        Owner
    }
}
