using Application.Common.Validation.Extensions;
using Application.TaskItems.DTOs;
using FluentValidation;

namespace Application.TaskItems.Validation
{
    public sealed class TaskItemDeleteDtoValidator : AbstractValidator<TaskItemDeleteDto>
    {
        public TaskItemDeleteDtoValidator()
        {
            RuleFor(t => t.Id).RequiredGuid();
            RuleFor(t => t.RowVersion).ConcurrencyTokenRules();
        }
    }
}
