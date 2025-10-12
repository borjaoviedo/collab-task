
namespace Application.Columns.DTOs
{
    public sealed class ColumnRenameDto
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public byte[] RowVersion { get; set; } = default!;
    }
}
