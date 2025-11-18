using Application.Columns.DTOs;

namespace TestHelpers.Api.Endpoints.Defaults
{
    public static class ColumnDefaults
    {
        public readonly static string DefaultColumnName = "column";
        public readonly static string DefaultColumnRename = "different name";
        public readonly static int DefaultColumnOrder = 0;
        public readonly static int DefaultColumnReorder = 1;

        public readonly static ColumnCreateDto DefaultColumnCreateDto = new()
        {
            Name = DefaultColumnName,
            Order = DefaultColumnOrder
        };

        public readonly static ColumnRenameDto DefaultColumnRenameDto = new() { NewName = DefaultColumnRename };

        public readonly static ColumnReorderDto DefaultColumnReorderDto = new() { NewOrder = DefaultColumnReorder };
    }
}
