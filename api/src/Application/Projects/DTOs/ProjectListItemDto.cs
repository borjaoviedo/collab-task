using Domain.Enums;
using System.Text.Json.Serialization;

namespace Application.Projects.DTOs
{
    public sealed class ProjectListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string Slug { get; set; } = default!;
        public DateTimeOffset UpdatedAt { get; set; }
        public int MembersCount { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))] public ProjectRole CurrentUserRole { get; set; }
    }
}
