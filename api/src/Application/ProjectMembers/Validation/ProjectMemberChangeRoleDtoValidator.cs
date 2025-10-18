using Application.Common.Validation.Extensions;
using Application.ProjectMembers.DTOs;
using FluentValidation;

namespace Application.ProjectMembers.Validation
{
    public sealed class ProjectMemberChangeRoleDtoValidator : AbstractValidator<ProjectMemberChangeRoleDto>
    {
        public ProjectMemberChangeRoleDtoValidator()
        {
            RuleFor(pm => pm.NewRole).ProjectRoleRules();
        }
    }
}
