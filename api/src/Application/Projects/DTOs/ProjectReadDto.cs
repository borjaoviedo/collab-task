using Domain.Enums;
using System.Text.Json.Serialization;

namespace Application.Projects.DTOs
{
    public sealed class ProjectReadDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string Slug { get; set; } = default!;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public byte[] RowVersion { get; set; } = default!;
        public int MembersCount { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))] public ProjectRole CurrentUserRole { get; set; }
    }
}
