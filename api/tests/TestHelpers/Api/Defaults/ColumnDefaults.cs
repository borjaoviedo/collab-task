using Application.Columns.DTOs;

namespace TestHelpers.Api.Defaults
{
    public static class ColumnDefaults
    {
        public readonly static string DefaultColumnName = "column";
        public readonly static int DefaultColumnOrder = 0;

        public readonly static ColumnCreateDto DefaultColumnCreateDto = new()
        {
            Name = DefaultColumnName,
            Order = DefaultColumnOrder
        };

        public readonly static string DefaultColumnRenameName = "different name";

        public readonly static ColumnRenameDto DefaultColumnRenameDto = new() { NewName = DefaultColumnRenameName };

    }
}
