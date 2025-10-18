using Application.Common.Validation.Extensions;
using Application.TaskItems.DTOs;
using FluentValidation;

namespace Application.TaskItems.Validation
{
    public sealed class TaskItemEditDtoValidator : AbstractValidator<TaskItemEditDto>
    {
        public TaskItemEditDtoValidator()
        {
            When(t => t.NewTitle is not null, () => RuleFor(t => t.NewTitle!).TaskTitleRules());
            When(t => t.NewDescription is not null, () => RuleFor(t => t.NewDescription!).TaskDescriptionRules());
        }
    }
}
