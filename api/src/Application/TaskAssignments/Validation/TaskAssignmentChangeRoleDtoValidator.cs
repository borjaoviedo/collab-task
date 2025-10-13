using Application.Common.Validation.Extensions;
using Application.TaskAssignments.DTOs;
using FluentValidation;

namespace Application.TaskAssignments.Validation
{
    public sealed class TaskAssignmentChangeRoleDtoValidator : AbstractValidator<TaskAssignmentChangeRoleDto>
    {
        public TaskAssignmentChangeRoleDtoValidator()
        {
            RuleFor(a => a.UserId).RequiredGuid();
            RuleFor(a => a.NewRole).TaskRoleRules();
        }
    }
}
