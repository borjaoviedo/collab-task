using Domain.Enums;
using System.Text.Json.Serialization;

namespace Application.ProjectMembers.DTOs
{
    public sealed class ProjectMemberReadDto
    {
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = default!;
        public string Email { get; set; } = default!;
        [JsonConverter(typeof(JsonStringEnumConverter))] public ProjectRole Role { get; set; }
        public DateTimeOffset JoinedAt { get; set; }
        public DateTimeOffset? RemovedAt { get; set; }
        public byte[] RowVersion { get; set; } = default!;
    }
}
