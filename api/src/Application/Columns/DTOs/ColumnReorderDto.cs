
namespace Application.Columns.DTOs
{
    public sealed class ColumnReorderDto
    {
        public Guid Id { get; set; }
        public int NewOrder { get; set; }
        public byte[] RowVersion { get; set; } = default!;
    }
}
