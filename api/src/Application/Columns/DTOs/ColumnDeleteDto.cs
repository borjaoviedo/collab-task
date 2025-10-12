
namespace Application.Columns.DTOs
{
    public sealed class ColumnDeleteDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = default!;
    }
}
