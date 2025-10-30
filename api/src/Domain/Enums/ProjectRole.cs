using System.Text.Json.Serialization;

namespace Domain.Enums
{
    /// <summary>
    /// Defines the access roles available within a project.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ProjectRole
    {
        Reader,
        Member,
        Admin,
        Owner
    }
}
