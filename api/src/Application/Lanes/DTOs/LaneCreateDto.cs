
namespace Application.Lanes.DTOs
{
    public sealed class LaneCreateDto
    {
        public required string Name { get; init; }
        public required int Order { get; init; }
    }
}
