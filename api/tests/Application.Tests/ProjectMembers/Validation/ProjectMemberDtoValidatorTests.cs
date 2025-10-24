using Application.ProjectMembers.DTOs;
using Application.ProjectMembers.Validation;
using Domain.Enums;
using FluentValidation.TestHelper;

namespace Application.Tests.ProjectMembers.Validation
{
    public sealed class ProjectMemberDtoValidatorTests
    {
        [Fact]
        public void Create_Valid_Passes()
        {
            var v = new ProjectMemberCreateDtoValidator();
            var dto = new ProjectMemberCreateDto { UserId = Guid.NewGuid(), Role = ProjectRole.Admin };

            var r = v.TestValidate(dto);
            r.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Create_Invalid_Fails()
        {
            var v = new ProjectMemberCreateDtoValidator();
            var dto = new ProjectMemberCreateDto { UserId = Guid.Empty, Role = (ProjectRole)999 };

            var r = v.TestValidate(dto);
            r.ShouldHaveValidationErrorFor(m => m.UserId)
             .WithErrorMessage("Id is required.");
            r.ShouldHaveValidationErrorFor(m => m.Role)
             .WithErrorMessage("Invalid project role value.");
        }

        [Fact]
        public void ChangeRole_Valid_Passes()
        {
            var v = new ProjectMemberChangeRoleDtoValidator();
            var dto = new ProjectMemberChangeRoleDto { NewRole = ProjectRole.Member };

            var r = v.TestValidate(dto);
            r.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void ChangeRole_Invalid_Fails()
        {
            var v = new ProjectMemberChangeRoleDtoValidator();
            var dto = new ProjectMemberChangeRoleDto { NewRole = (ProjectRole)999 };

            var r = v.TestValidate(dto);
            r.ShouldHaveValidationErrorFor(m => m.NewRole)
             .WithErrorMessage("Invalid project role value.");
        }
    }
}
