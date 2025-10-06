using Domain.Enums;
using System.Text.Json.Serialization;

namespace Application.Users.DTOs
{
    public sealed class UserReadDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = default!;
        public string Name { get; set; } = default!;
        [JsonConverter(typeof(JsonStringEnumConverter))] public UserRole Role { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public int ProjectMembershipsCount { get; set; }
        public byte[] RowVersion { get; set; } = default!;
    }
}
