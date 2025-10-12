using Application.Common.Validation.Extensions;
using Application.TaskAssignments.DTOs;
using FluentValidation;

namespace Application.TaskAssignments.Validation
{
    public sealed class TaskAssignmentDeleteDtoValidator : AbstractValidator<TaskAssignmentDeleteDto>
    {
        public TaskAssignmentDeleteDtoValidator()
        {
            RuleFor(a => a.TaskId).RequiredGuid();
            RuleFor(a => a.UserId).RequiredGuid();
            RuleFor(a => a.RowVersion).ConcurrencyTokenRules();
        }
    }
}
