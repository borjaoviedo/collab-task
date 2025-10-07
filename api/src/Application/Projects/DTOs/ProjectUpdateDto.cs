
namespace Application.Projects.DTOs
{
    public sealed class ProjectUpdateDto
    {
        public string? Name { get; set; }
        public byte[] RowVersion { get; set; } = default!;
    }
}
