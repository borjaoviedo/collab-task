
namespace Application.Lanes.DTOs
{
    public sealed class LaneReadDto
    {
        public Guid Id { get; init; }
        public Guid ProjectId { get; init; }
        public string Name { get; init; } = default!;
        public int Order { get; init; }
        public string RowVersion { get; init; } = default!;
    }
}
