
namespace Application.Lanes.DTOs
{
    public sealed class LaneReorderDto
    {
        public Guid Id { get; set; }
        public int NewOrder { get; set; }
        public byte[] RowVersion { get; set; } = default!;
    }
}
