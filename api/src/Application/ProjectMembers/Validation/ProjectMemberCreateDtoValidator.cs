using Application.Common.Validation.Extensions;
using Application.ProjectMembers.DTOs;
using FluentValidation;

namespace Application.ProjectMembers.Validation
{
    public sealed class ProjectMemberCreateDtoValidator : AbstractValidator<ProjectMemberCreateDto>
    {
        public ProjectMemberCreateDtoValidator()
        {
            RuleFor(pm => pm.UserId).RequiredGuid();
            RuleFor(pm => pm.Role).ProjectRoleRules();
        }
    }
}
