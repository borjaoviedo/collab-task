using Domain.Enums;
using System.Text.Json.Serialization;

namespace Application.Users.DTOs
{
    public sealed class UserReadDto
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = default!;
        public string Name { get; init; } = default!;
        [JsonConverter(typeof(JsonStringEnumConverter))] public UserRole Role { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset UpdatedAt { get; init; }
        public int ProjectMembershipsCount { get; init; }
        public byte[] RowVersion { get; init; } = default!;
    }
}
