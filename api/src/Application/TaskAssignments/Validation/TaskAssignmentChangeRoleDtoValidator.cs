using Application.Common.Validation.Extensions;
using Application.TaskAssignments.DTOs;
using FluentValidation;

namespace Application.TaskAssignments.Validation
{
    public sealed class TaskAssignmentChangeRoleDtoValidator : AbstractValidator<TaskAssignmentChangeRoleDto>
    {
        public TaskAssignmentChangeRoleDtoValidator()
        {
            RuleFor(a => a.TaskId).RequiredGuid();
            RuleFor(a => a.UserId).RequiredGuid();
            RuleFor(a => a.NewRole).TaskRoleRules();
            RuleFor(a => a.RowVersion).ConcurrencyTokenRules();
        }
    }
}
