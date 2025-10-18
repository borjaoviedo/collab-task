using Domain.Enums;
using System.Text.Json.Serialization;

namespace Application.ProjectMembers.DTOs
{
    public sealed class ProjectMemberReadDto
    {
        public Guid ProjectId { get; init; }
        public Guid UserId { get; init; }
        public string UserName { get; init; } = default!;
        public string Email { get; init; } = default!;
        [JsonConverter(typeof(JsonStringEnumConverter))] public ProjectRole Role { get; init; }
        public DateTimeOffset JoinedAt { get; init; }
        public DateTimeOffset? RemovedAt { get; init; }
        public byte[] RowVersion { get; init; } = default!;
    }
}
