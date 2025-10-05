using Application.Common.Validation.Extensions;
using Application.ProjectMembers.DTOs;
using FluentValidation;

namespace Application.ProjectMembers.Validation
{
    public sealed class ProjectMemberRemoveDtoValidator : AbstractValidator<ProjectMemberRemoveDto>
    {
        public ProjectMemberRemoveDtoValidator()
        {
            RuleFor(pm => pm.RemovedAt).RemovedAtRules();
            RuleFor(pm => pm.RowVersion).ConcurrencyTokenRules();
        }
    }
}
