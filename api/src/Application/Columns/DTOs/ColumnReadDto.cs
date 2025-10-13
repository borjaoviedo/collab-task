
namespace Application.Columns.DTOs
{
    public sealed class ColumnReadDto
    {
        public Guid Id { get; set; }
        public Guid LaneId { get; set; }
        public Guid ProjectId { get; set; }
        public string Name { get; set; } = default!;
        public int Order { get; set; }
        public byte[] RowVersion { get; set; } = default!;
    }
}
