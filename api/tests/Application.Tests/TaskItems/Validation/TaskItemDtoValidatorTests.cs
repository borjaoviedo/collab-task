using Application.TaskItems.DTOs;
using Application.TaskItems.Validation;
using FluentValidation.TestHelper;

namespace Application.Tests.TaskItems.Validation
{
    public sealed class TaskItemDtoValidatorTests
    {
        [Fact]
        public void Create_Valid_Passes()
        {
            var v = new TaskItemCreateDtoValidator();
            var dto = new TaskItemCreateDto
            {
                Title = "Title",
                Description = "Description",
                DueDate = null,
                SortKey = 0m
            };
            v.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Create_Past_DueDate_Fails()
        {
            var v = new TaskItemCreateDtoValidator();
            var dto = new TaskItemCreateDto
            {
                Title = "Title",
                Description = "Description",
                DueDate = DateTimeOffset.UtcNow.AddMinutes(-1),
                SortKey = 0m
            };
            v.TestValidate(dto).ShouldHaveValidationErrorFor(t => t.DueDate);
        }

        [Fact]
        public void Edit_Optional_Fields_Respect_Rules()
        {
            var v = new TaskItemEditDtoValidator();
            var dto = new TaskItemEditDto
            {
                NewTitle = null, // optional
                NewDescription = "", // invalid if provided
                NewDueDate = null // valid
            };
            var r = v.TestValidate(dto);
            r.ShouldHaveValidationErrorFor(t => t.NewDescription);
            r.ShouldNotHaveValidationErrorFor(t => t.NewTitle);
            r.ShouldNotHaveValidationErrorFor(t => t.NewDueDate);
        }

        [Fact]
        public void Move_Invalid_Targets_Fail()
        {
            var v = new TaskItemMoveDtoValidator();
            var dto = new TaskItemMoveDto
            {
                NewLaneId = Guid.Empty,
                NewColumnId = Guid.Empty,
                NewSortKey = -1m,
            };
            var r = v.TestValidate(dto);
            r.ShouldHaveValidationErrorFor(t => t.NewLaneId);
            r.ShouldHaveValidationErrorFor(t => t.NewColumnId);
            r.ShouldHaveValidationErrorFor(t => t.NewSortKey);
        }
    }
}
