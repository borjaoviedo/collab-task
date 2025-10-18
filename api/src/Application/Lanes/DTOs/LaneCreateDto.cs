
namespace Application.Lanes.DTOs
{
    public sealed class LaneCreateDto
    {
        public required string Name { get; set; }
        public required int Order { get; set; }
    }
}
