
namespace Application.Lanes.DTOs
{
    public sealed class LaneRenameDto
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public byte[] RowVersion { get; set; } = default!;
    }
}
