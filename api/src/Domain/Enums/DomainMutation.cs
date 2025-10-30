using System.Text.Json.Serialization;

namespace Domain.Enums
{
    /// <summary>
    /// Represents the possible results of a domain-level mutation operation.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DomainMutation
    {
        NoOp,
        NotFound,
        Updated,
        Created,
        Deleted,
        Conflict
    }
}
