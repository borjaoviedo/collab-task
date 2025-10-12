
namespace Application.Columns.DTOs
{
    public sealed class ColumnCreateDto
    {
        public Guid LaneId { get; set; }
        public Guid ProjectId { get; set; }
        public required string Name { get; set; }
        public int Order { get; set; }
    }
}
