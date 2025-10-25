using System.Text.Json.Serialization;

namespace Domain.Enums
{
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
