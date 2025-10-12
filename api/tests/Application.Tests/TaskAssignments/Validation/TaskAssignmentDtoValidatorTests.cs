using Application.TaskAssignments.DTOs;
using Application.TaskAssignments.Validation;
using Domain.Enums;
using FluentValidation.TestHelper;

namespace Application.Tests.TaskAssignments.Validation
{
    public sealed class TaskAssignmentDtoValidatorTests
    {
        [Fact]
        public void Create_Invalid_Fails()
        {
            var v = new TaskAssignmentCreateDtoValidator();
            var dto = new TaskAssignmentCreateDto { TaskId = Guid.Empty, UserId = Guid.Empty, Role = (TaskRole)999 };
            var r = v.TestValidate(dto);
            r.ShouldHaveValidationErrorFor(x => x.TaskId);
            r.ShouldHaveValidationErrorFor(x => x.UserId);
            r.ShouldHaveValidationErrorFor(x => x.Role);
        }

        [Fact]
        public void ChangeRole_Missing_RowVersion_Fails()
        {
            var v = new TaskAssignmentChangeRoleDtoValidator();
            var dto = new TaskAssignmentChangeRoleDto
            {
                TaskId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                NewRole = TaskRole.CoOwner,
                RowVersion = []
            };
            v.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.RowVersion);
        }

        [Fact]
        public void Delete_Missing_RowVersion_Fails()
        {
            var v = new TaskAssignmentDeleteDtoValidator();
            var dto = new TaskAssignmentDeleteDto { TaskId = Guid.NewGuid(), UserId = Guid.NewGuid(), RowVersion = [] };
            v.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.RowVersion);
        }
    }
}
