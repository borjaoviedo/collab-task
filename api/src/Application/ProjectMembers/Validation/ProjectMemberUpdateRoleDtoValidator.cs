using Application.Common.Validation.Extensions;
using Application.ProjectMembers.DTOs;
using FluentValidation;

namespace Application.ProjectMembers.Validation
{
    public sealed class ProjectMemberUpdateRoleDtoValidator : AbstractValidator<ProjectMemberUpdateRoleDto>
    {
        public ProjectMemberUpdateRoleDtoValidator()
        {
            RuleFor(pm => pm.Role).ProjectRoleRules();
            RuleFor(pm => pm.RowVersion).ConcurrencyTokenRules();
        }
    }
}
