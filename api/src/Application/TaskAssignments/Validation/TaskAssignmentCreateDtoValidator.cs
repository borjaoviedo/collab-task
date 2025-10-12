using Application.Common.Validation.Extensions;
using Application.TaskAssignments.DTOs;
using FluentValidation;

namespace Application.TaskAssignments.Validation
{
    public sealed class TaskAssignmentCreateDtoValidator : AbstractValidator<TaskAssignmentCreateDto>
    {
        public TaskAssignmentCreateDtoValidator()
        {
            RuleFor(a => a.TaskId).RequiredGuid();
            RuleFor(a => a.UserId).RequiredGuid();
            RuleFor(a => a.Role).TaskRoleRules();
        }
    }
}
