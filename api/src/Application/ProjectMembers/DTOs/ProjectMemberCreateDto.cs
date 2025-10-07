using Domain.Enums;
using System.Text.Json.Serialization;

namespace Application.ProjectMembers.DTOs
{
    public sealed class ProjectMemberCreateDto
    {
        public Guid UserId { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))] public ProjectRole Role { get; set; }
        public DateTimeOffset JoinedAt { get; set; }
    }
}
