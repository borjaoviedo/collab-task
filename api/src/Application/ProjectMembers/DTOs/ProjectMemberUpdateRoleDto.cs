using Domain.Enums;
using System.Text.Json.Serialization;

namespace Application.ProjectMembers.DTOs
{
    public sealed class ProjectMemberUpdateRoleDto
    {
        [JsonConverter(typeof(JsonStringEnumConverter))] public ProjectRole Role { get; set; }
        public byte[] RowVersion { get; set; } = default!;
    }
}
