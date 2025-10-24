using Application.TaskAssignments.DTOs;
using Application.TaskAssignments.Validation;
using Domain.Enums;
using FluentValidation.TestHelper;

namespace Application.Tests.TaskAssignments.Validation
{
    public sealed class TaskAssignmentDtoValidatorTests
    {
        [Fact]
        public void Create_Valid_Passes()
        {
            var v = new TaskAssignmentCreateDtoValidator();
            var dto = new TaskAssignmentCreateDto { UserId = Guid.NewGuid(), Role = TaskRole.Owner };
            var r = v.TestValidate(dto);
            r.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Create_Invalid_Fails()
        {
            var v = new TaskAssignmentCreateDtoValidator();
            var dto = new TaskAssignmentCreateDto { UserId = Guid.Empty, Role = (TaskRole)999 };
            var r = v.TestValidate(dto);
            r.ShouldHaveValidationErrorFor(a => a.UserId)
                .WithErrorMessage("Id is required.");
            r.ShouldHaveValidationErrorFor(a => a.Role)
                .WithErrorMessage("Invalid task role value.");
        }

        [Fact]
        public void ChangeRole_Valid_Passes()
        {
            var v = new TaskAssignmentChangeRoleDtoValidator();
            var dto = new TaskAssignmentChangeRoleDto { NewRole = TaskRole.CoOwner };
            var r = v.TestValidate(dto);
            r.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void ChangeRole_Invalid_Fails()
        {
            var v = new TaskAssignmentChangeRoleDtoValidator();
            var dto = new TaskAssignmentChangeRoleDto { NewRole = (TaskRole)999 };
            var r = v.TestValidate(dto);
            r.ShouldHaveValidationErrorFor(a => a.NewRole)
                .WithErrorMessage("Invalid task role value.");
        }
    }
}
