using System.Text.Json.Serialization;

namespace Domain.Enums
{
    /// <summary>
    /// Defines the system-wide roles assignable to a user.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum UserRole
    {
        User,
        Admin
    }
}
