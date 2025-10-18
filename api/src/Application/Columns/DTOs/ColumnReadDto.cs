
namespace Application.Columns.DTOs
{
    public sealed class ColumnReadDto
    {
        public Guid Id { get; init; }
        public Guid LaneId { get; init; }
        public Guid ProjectId { get; init; }
        public string Name { get; init; } = default!;
        public int Order { get; init; }
        public byte[] RowVersion { get; init; } = default!;
    }
}
