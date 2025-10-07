using Domain.Enums;
using System.Text.Json.Serialization;

namespace Api.Auth.DTOs
{
    public sealed class MeReadDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = default!;
        public string Name { get; set; } = default!;
        [JsonConverter(typeof(JsonStringEnumConverter))] public UserRole Role { get; set; }
        public int ProjectMembershipsCount { get; set; }
    }
}
