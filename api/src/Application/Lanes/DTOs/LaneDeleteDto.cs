
namespace Application.Lanes.DTOs
{
    public sealed class LaneDeleteDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = default!;
    }
}
