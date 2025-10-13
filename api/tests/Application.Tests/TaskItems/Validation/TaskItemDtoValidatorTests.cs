using Application.TaskItems.DTOs;
using Application.TaskItems.Validation;
using FluentValidation.TestHelper;

namespace Application.Tests.TaskItems.Validation
{
    public sealed class TaskItemDtoValidatorTests
    {
        [Fact]
        public void Create_Past_DueDate_Fails()
        {
            var v = new TaskItemCreateDtoValidator();
            var dto = new TaskItemCreateDto
            {
                Title = "A",
                Description = "D",
                DueDate = DateTimeOffset.UtcNow.AddMinutes(-1)
            };
            v.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.DueDate);
        }

        [Fact]
        public void Edit_Optional_Fields_Respect_Rules()
        {
            var v = new TaskItemEditDtoValidator();
            var dto = new TaskItemEditDto
            {
                Title = null, // optional
                Description = "", // invalid if provided
            };
            var r = v.TestValidate(dto);
            r.ShouldHaveValidationErrorFor(x => x.Description);
            r.ShouldNotHaveValidationErrorFor(x => x.Title);
        }

        [Fact]
        public void Move_Invalid_Targets_Fail()
        {
            var v = new TaskItemMoveDtoValidator();
            var dto = new TaskItemMoveDto
            {
                TargetLaneId = Guid.Empty,
                TargetColumnId = Guid.Empty,
                TargetSortKey = -1m,
            };
            var r = v.TestValidate(dto);
            r.ShouldHaveValidationErrorFor(x => x.TargetLaneId);
            r.ShouldHaveValidationErrorFor(x => x.TargetColumnId);
            r.ShouldHaveValidationErrorFor(x => x.TargetSortKey);
        }
    }
}
