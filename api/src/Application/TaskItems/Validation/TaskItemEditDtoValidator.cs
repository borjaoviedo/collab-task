using Application.Common.Validation.Extensions;
using Application.TaskItems.DTOs;
using FluentValidation;

namespace Application.TaskItems.Validation
{
    public sealed class TaskItemEditDtoValidator : AbstractValidator<TaskItemEditDto>
    {
        public TaskItemEditDtoValidator()
        {
            When(t => t.Title is not null, () => RuleFor(t => t.Title!).TaskTitleRules());
            When(t => t.Description is not null, () => RuleFor(t => t.Description!).TaskDescriptionRules());
        }
    }
}
