using Application.Common.Validation.Extensions;
using Application.TaskItems.DTOs;
using FluentValidation;

namespace Application.TaskItems.Validation
{
    public sealed class TaskItemCreateDtoValidator : AbstractValidator<TaskItemCreateDto>
    {
        public TaskItemCreateDtoValidator()
        {
            RuleFor(t => t.Title).TaskTitleRules();
            RuleFor(t => t.Description).TaskDescriptionRules();
            RuleFor(t => t.DueDate).DueDateRules();
            RuleFor(t => t.SortKey).NonNegativeSortKey();
        }
    }
}
