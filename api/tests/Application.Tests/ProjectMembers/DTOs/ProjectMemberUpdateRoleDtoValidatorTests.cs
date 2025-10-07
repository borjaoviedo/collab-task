using Application.ProjectMembers.DTOs;
using Application.ProjectMembers.Validation;
using Domain.Enums;
using FluentValidation.TestHelper;

namespace Application.Tests.ProjectMembers.DTOs
{
    public sealed class ProjectMemberUpdateRoleDtoValidatorTests
    {
        private readonly ProjectMemberUpdateRoleDtoValidator _validator = new();
        [Fact]
        public void Role_Invalid_Fails()
        {
            _validator.TestValidate(new ProjectMemberUpdateRoleDto { Role = (ProjectRole)999, RowVersion = [1] })
                .ShouldHaveValidationErrorFor(x => x.Role);
        }

        [Fact]
        public void RowVersion_Empty_Fails()
        {
            _validator.TestValidate(new ProjectMemberUpdateRoleDto { Role = (ProjectRole)1, RowVersion = [] })
                .ShouldHaveValidationErrorFor(x => x.RowVersion);
        }

        [Fact]
        public void Valid_Role_And_RowVersion_Passes()
        {
            _validator.TestValidate(new ProjectMemberUpdateRoleDto { Role = (ProjectRole)2, RowVersion = [10] })
                .ShouldNotHaveAnyValidationErrors();
        }
    }
}
