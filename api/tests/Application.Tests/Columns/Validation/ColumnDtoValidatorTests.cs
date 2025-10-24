using Application.Columns.DTOs;
using Application.Columns.Validation;
using FluentValidation.TestHelper;

namespace Application.Tests.Columns.Validation
{
    public sealed class ColumnDtoValidatorTests
    {

        [Fact]
        public void Create_Valid_Passes()
        {
            var v = new ColumnCreateDtoValidator();
            var dto = new ColumnCreateDto { Name = "Backlog", Order = 0 };

            var r = v.TestValidate(dto);
            r.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Create_Invalid_Fails()
        {
            var v = new ColumnCreateDtoValidator();
            var dto = new ColumnCreateDto { Name = "  ", Order = -1 };

            var r = v.TestValidate(dto);
            r.ShouldHaveValidationErrorFor(c => c.Name)
             .WithErrorMessage("Column name cannot be whitespace.");
            r.ShouldHaveValidationErrorFor(c => c.Order)
             .WithErrorMessage("Column order must be ≥ 0.");
        }

        [Fact]
        public void Rename_Valid_Passes()
        {
            var v = new ColumnRenameDtoValidator();
            var dto = new ColumnRenameDto { NewName = "Valid" };

            var r = v.TestValidate(dto);
            r.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Rename_Invalid_Fails()
        {
            var v = new ColumnRenameDtoValidator();
            var dto = new ColumnRenameDto { NewName = "  " };

            var r = v.TestValidate(dto);
            r.ShouldHaveValidationErrorFor(c => c.NewName)
             .WithErrorMessage("Column name cannot be whitespace.");
        }

        [Fact]
        public void Reorder_Valid_Passes()
        {
            var v = new ColumnReorderDtoValidator();
            var dto = new ColumnReorderDto { NewOrder = 1 };

            var r = v.TestValidate(dto);
            r.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Reorder_Invalid_Fails()
        {
            var v = new ColumnReorderDtoValidator();
            var dto = new ColumnReorderDto { NewOrder = -1 };

            var r = v.TestValidate(dto);
            r.ShouldHaveValidationErrorFor(c => c.NewOrder)
             .WithErrorMessage("Column order must be ≥ 0.");
        }
    }
}
