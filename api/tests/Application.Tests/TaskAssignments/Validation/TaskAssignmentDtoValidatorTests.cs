using Application.TaskAssignments.DTOs;
using Application.TaskAssignments.Validation;
using Domain.Enums;
using FluentValidation.TestHelper;
using TestHelpers.Common.Testing;

namespace Application.Tests.TaskAssignments.Validation
{
    [UnitTest]
    public sealed class TaskAssignmentDtoValidatorTests
    {
        [Fact]
        public void Create_Valid_Passes()
        {
            var validator = new TaskAssignmentCreateDtoValidator();
            var dto = new TaskAssignmentCreateDto { UserId = Guid.NewGuid(), Role = TaskRole.Owner };
            var validationResult = validator.TestValidate(dto);

            validationResult.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Create_Invalid_Fails()
        {
            var validator = new TaskAssignmentCreateDtoValidator();
            var dto = new TaskAssignmentCreateDto { UserId = Guid.Empty, Role = (TaskRole)999 };
            var validationResult = validator.TestValidate(dto);

            validationResult.ShouldHaveValidationErrorFor(a => a.UserId)
                .WithErrorMessage("Id is required.");
            validationResult.ShouldHaveValidationErrorFor(a => a.Role)
                .WithErrorMessage("Invalid task role value.");
        }

        [Fact]
        public void ChangeRole_Valid_Passes()
        {
            var validator = new TaskAssignmentChangeRoleDtoValidator();
            var dto = new TaskAssignmentChangeRoleDto { NewRole = TaskRole.CoOwner };
            var validationResult = validator.TestValidate(dto);

            validationResult.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void ChangeRole_Invalid_Fails()
        {
            var validator = new TaskAssignmentChangeRoleDtoValidator();
            var dto = new TaskAssignmentChangeRoleDto { NewRole = (TaskRole)999 };
            var validationResult = validator.TestValidate(dto);

            validationResult.ShouldHaveValidationErrorFor(a => a.NewRole)
                .WithErrorMessage("Invalid task role value.");
        }
    }
}
