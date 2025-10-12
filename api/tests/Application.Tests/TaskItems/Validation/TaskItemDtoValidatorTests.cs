using Application.TaskItems.DTOs;
using Application.TaskItems.Validation;
using FluentValidation.TestHelper;

namespace Application.Tests.TaskItems.Validation
{
    public sealed class TaskItemDtoValidatorTests
    {
        [Fact]
        public void Create_Invalid_Ids_Fail()
        {
            var v = new TaskItemCreateDtoValidator();
            var dto = new TaskItemCreateDto
            {
                ProjectId = Guid.Empty,
                LaneId = Guid.Empty,
                ColumnId = Guid.Empty,
                Title = "A",
                Description = "D"
            };
            var r = v.TestValidate(dto);
            r.ShouldHaveValidationErrorFor(x => x.ProjectId);
            r.ShouldHaveValidationErrorFor(x => x.LaneId);
            r.ShouldHaveValidationErrorFor(x => x.ColumnId);
        }

        [Fact]
        public void Create_Past_DueDate_Fails()
        {
            var v = new TaskItemCreateDtoValidator();
            var dto = new TaskItemCreateDto
            {
                ProjectId = Guid.NewGuid(),
                LaneId = Guid.NewGuid(),
                ColumnId = Guid.NewGuid(),
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
                Id = Guid.NewGuid(),
                Title = null, // optional
                Description = "", // invalid if provided
                RowVersion =[1]
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
                Id = Guid.Empty,
                TargetLaneId = Guid.Empty,
                TargetColumnId = Guid.Empty,
                TargetSortKey = -1m,
                RowVersion = []
            };
            var r = v.TestValidate(dto);
            r.ShouldHaveValidationErrorFor(x => x.Id);
            r.ShouldHaveValidationErrorFor(x => x.TargetLaneId);
            r.ShouldHaveValidationErrorFor(x => x.TargetColumnId);
            r.ShouldHaveValidationErrorFor(x => x.TargetSortKey);
            r.ShouldHaveValidationErrorFor(x => x.RowVersion);
        }
    }
}
