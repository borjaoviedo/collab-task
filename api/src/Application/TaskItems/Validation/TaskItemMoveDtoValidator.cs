using Application.Common.Validation.Extensions;
using Application.TaskItems.DTOs;
using FluentValidation;

namespace Application.TaskItems.Validation
{
    public sealed class TaskItemMoveDtoValidator : AbstractValidator<TaskItemMoveDto>
    {
        public TaskItemMoveDtoValidator()
        {
            RuleFor(t => t.NewColumnId).RequiredGuid();
            RuleFor(t => t.NewLaneId).RequiredGuid();
            RuleFor(t => t.NewSortKey).NonNegativeSortKey();
        }
    }
}
