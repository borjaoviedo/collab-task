using Application.ProjectMembers.DTOs;
using Application.ProjectMembers.Validation;
using Domain.Enums;
using FluentValidation.TestHelper;
using TestHelpers.Common.Testing;

namespace Application.Tests.ProjectMembers.Validation
{
    [UnitTest]
    public sealed class ProjectMemberDtoValidatorTests
    {
        [Fact]
        public void Create_Valid_Passes()
        {
            var validator = new ProjectMemberCreateDtoValidator();
            var dto = new ProjectMemberCreateDto { UserId = Guid.NewGuid(), Role = ProjectRole.Admin };

            var validationResult = validator.TestValidate(dto);
            validationResult.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Create_Invalid_Fails()
        {
            var validator = new ProjectMemberCreateDtoValidator();
            var dto = new ProjectMemberCreateDto { UserId = Guid.Empty, Role = (ProjectRole)999 };

            var validationResult = validator.TestValidate(dto);
            validationResult.ShouldHaveValidationErrorFor(m => m.UserId)
             .WithErrorMessage("Id is required.");
            validationResult.ShouldHaveValidationErrorFor(m => m.Role)
             .WithErrorMessage("Invalid project role value.");
        }

        [Fact]
        public void ChangeRole_Valid_Passes()
        {
            var validator = new ProjectMemberChangeRoleDtoValidator();
            var dto = new ProjectMemberChangeRoleDto { NewRole = ProjectRole.Member };

            var validationResult = validator.TestValidate(dto);
            validationResult.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void ChangeRole_Invalid_Fails()
        {
            var validator = new ProjectMemberChangeRoleDtoValidator();
            var dto = new ProjectMemberChangeRoleDto { NewRole = (ProjectRole)999 };

            var validationResult = validator.TestValidate(dto);
            validationResult.ShouldHaveValidationErrorFor(m => m.NewRole)
             .WithErrorMessage("Invalid project role value.");
        }
    }
}
