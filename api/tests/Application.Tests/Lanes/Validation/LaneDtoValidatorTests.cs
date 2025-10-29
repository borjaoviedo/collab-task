using Application.Lanes.DTOs;
using Application.Lanes.Validation;
using FluentValidation.TestHelper;

namespace Application.Tests.Lanes.Validation
{
    public sealed class LaneDtoValidatorTests
    {
        [Fact]
        public void Create_Valid_Passes()
        {
            var validator = new LaneCreateDtoValidator();
            var dto = new LaneCreateDto { Name = "Backlog", Order = 0 };

            var validationResult = validator.TestValidate(dto);
            validationResult.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Create_Invalid_Fails()
        {
            var validator = new LaneCreateDtoValidator();
            var dto = new LaneCreateDto { Name = "  ", Order = -1 };

            var validationResult = validator.TestValidate(dto);
            validationResult.ShouldHaveValidationErrorFor(l => l.Name)
             .WithErrorMessage("Lane name cannot be whitespace.");
            validationResult.ShouldHaveValidationErrorFor(l => l.Order)
             .WithErrorMessage("Lane order must be ≥ 0.");
        }

        [Fact]
        public void Rename_Valid_Passes()
        {
            var validator = new LaneRenameDtoValidator();
            var dto = new LaneRenameDto { NewName = "Valid" };

            var validationResult = validator.TestValidate(dto);
            validationResult.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Rename_Invalid_Fails()
        {
            var validator = new LaneRenameDtoValidator();
            var dto = new LaneRenameDto { NewName = "  " };

            var validationResult = validator.TestValidate(dto);
            validationResult.ShouldHaveValidationErrorFor(l => l.NewName)
             .WithErrorMessage("Lane name cannot be whitespace.");
        }

        [Fact]
        public void Reorder_Valid_Passes()
        {
            var validator = new LaneReorderDtoValidator();
            var dto = new LaneReorderDto { NewOrder = 1 };

            var validationResult = validator.TestValidate(dto);
            validationResult.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Reorder_Invalid_Fails()
        {
            var validator = new LaneReorderDtoValidator();
            var dto = new LaneReorderDto { NewOrder = -1 };

            var validationResult = validator.TestValidate(dto);
            validationResult.ShouldHaveValidationErrorFor(l => l.NewOrder)
             .WithErrorMessage("Lane order must be ≥ 0.");
        }
    }
}
