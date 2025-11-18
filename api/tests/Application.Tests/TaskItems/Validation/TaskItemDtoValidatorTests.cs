using Application.TaskItems.DTOs;
using Application.TaskItems.Validation;
using FluentValidation.TestHelper;
using TestHelpers.Common.Testing;

namespace Application.Tests.TaskItems.Validation
{
    [UnitTest]
    public sealed class TaskItemDtoValidatorTests
    {
        [Fact]
        public void Create_Valid_Passes()
        {
            var validator = new TaskItemCreateDtoValidator();
            var dto = new TaskItemCreateDto
            {
                Title = "Title",
                Description = "Description",
                DueDate = null,
                SortKey = 0m
            };
            validator.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Create_Past_DueDate_Fails()
        {
            var validator = new TaskItemCreateDtoValidator();
            var dto = new TaskItemCreateDto
            {
                Title = "Title",
                Description = "Description",
                DueDate = DateTimeOffset.UtcNow.AddMinutes(-1),
                SortKey = 0m
            };
            validator.TestValidate(dto).ShouldHaveValidationErrorFor(t => t.DueDate);
        }

        [Fact]
        public void Edit_Optional_Fields_Respect_Rules()
        {
            var validator = new TaskItemEditDtoValidator();
            var dto = new TaskItemEditDto
            {
                NewTitle = null, // optional
                NewDescription = "", // invalid if provided
                NewDueDate = null // valid
            };
            var validationResult = validator.TestValidate(dto);
            validationResult.ShouldHaveValidationErrorFor(t => t.NewDescription);
            validationResult.ShouldNotHaveValidationErrorFor(t => t.NewTitle);
            validationResult.ShouldNotHaveValidationErrorFor(t => t.NewDueDate);
        }

        [Fact]
        public void Move_Invalid_Targets_Fail()
        {
            var validator = new TaskItemMoveDtoValidator();
            var dto = new TaskItemMoveDto
            {
                NewLaneId = Guid.Empty,
                NewColumnId = Guid.Empty,
                NewSortKey = -1m,
            };
            var validationResult = validator.TestValidate(dto);
            validationResult.ShouldHaveValidationErrorFor(t => t.NewLaneId);
            validationResult.ShouldHaveValidationErrorFor(t => t.NewColumnId);
            validationResult.ShouldHaveValidationErrorFor(t => t.NewSortKey);
        }
    }
}
