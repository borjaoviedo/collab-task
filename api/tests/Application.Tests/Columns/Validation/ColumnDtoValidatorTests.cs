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
            var validator = new ColumnCreateDtoValidator();
            var dto = new ColumnCreateDto { Name = "Backlog", Order = 0 };

            var validationResult = validator.TestValidate(dto);
            validationResult.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Create_Invalid_Fails()
        {
            var validator = new ColumnCreateDtoValidator();
            var dto = new ColumnCreateDto { Name = "  ", Order = -1 };

            var validationResult = validator.TestValidate(dto);

            validationResult.ShouldHaveValidationErrorFor(c => c.Name)
                .WithErrorMessage("Column name cannot be whitespace.");
            validationResult.ShouldHaveValidationErrorFor(c => c.Order)
                .WithErrorMessage("Column order must be ≥ 0.");
        }

        [Fact]
        public void Rename_Valid_Passes()
        {
            var validator = new ColumnRenameDtoValidator();
            var dto = new ColumnRenameDto { NewName = "Valid" };

            var validationResult = validator.TestValidate(dto);
            validationResult.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Rename_Invalid_Fails()
        {
            var validator = new ColumnRenameDtoValidator();
            var dto = new ColumnRenameDto { NewName = "  " };

            var validationResult = validator.TestValidate(dto);
            validationResult.ShouldHaveValidationErrorFor(c => c.NewName)
                .WithErrorMessage("Column name cannot be whitespace.");
        }

        [Fact]
        public void Reorder_Valid_Passes()
        {
            var validator = new ColumnReorderDtoValidator();
            var dto = new ColumnReorderDto { NewOrder = 1 };

            var validationResult = validator.TestValidate(dto);
            validationResult.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Reorder_Invalid_Fails()
        {
            var validator = new ColumnReorderDtoValidator();
            var dto = new ColumnReorderDto { NewOrder = -1 };

            var validationResult = validator.TestValidate(dto);
            validationResult.ShouldHaveValidationErrorFor(c => c.NewOrder)
                .WithErrorMessage("Column order must be ≥ 0.");
        }
    }
}
