using System.Text.Json.Serialization;

namespace Domain.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ProjectRole
    {
        Reader,
        Member,
        Admin,
        Owner
    }
}
