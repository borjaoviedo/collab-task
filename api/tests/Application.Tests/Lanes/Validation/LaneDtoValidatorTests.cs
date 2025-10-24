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
            var v = new LaneCreateDtoValidator();
            var dto = new LaneCreateDto { Name = "Backlog", Order = 0 };

            var r = v.TestValidate(dto);
            r.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Create_Invalid_Fails()
        {
            var v = new LaneCreateDtoValidator();
            var dto = new LaneCreateDto { Name = "  ", Order = -1 };

            var r = v.TestValidate(dto);
            r.ShouldHaveValidationErrorFor(l => l.Name)
             .WithErrorMessage("Lane name cannot be whitespace.");
            r.ShouldHaveValidationErrorFor(l => l.Order)
             .WithErrorMessage("Lane order must be ≥ 0.");
        }

        [Fact]
        public void Rename_Valid_Passes()
        {
            var v = new LaneRenameDtoValidator();
            var dto = new LaneRenameDto { NewName = "Valid" };

            var r = v.TestValidate(dto);
            r.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Rename_Invalid_Fails()
        {
            var v = new LaneRenameDtoValidator();
            var dto = new LaneRenameDto { NewName = "  " };

            var r = v.TestValidate(dto);
            r.ShouldHaveValidationErrorFor(l => l.NewName)
             .WithErrorMessage("Lane name cannot be whitespace.");
        }

        [Fact]
        public void Reorder_Valid_Passes()
        {
            var v = new LaneReorderDtoValidator();
            var dto = new LaneReorderDto { NewOrder = 1 };

            var r = v.TestValidate(dto);
            r.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Reorder_Invalid_Fails()
        {
            var v = new LaneReorderDtoValidator();
            var dto = new LaneReorderDto { NewOrder = -1 };

            var r = v.TestValidate(dto);
            r.ShouldHaveValidationErrorFor(l => l.NewOrder)
             .WithErrorMessage("Lane order must be ≥ 0.");
        }
    }
}
