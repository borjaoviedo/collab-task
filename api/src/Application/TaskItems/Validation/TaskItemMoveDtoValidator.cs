using Application.Common.Validation.Extensions;
using Application.TaskItems.DTOs;
using FluentValidation;

namespace Application.TaskItems.Validation
{
    public sealed class TaskItemMoveDtoValidator : AbstractValidator<TaskItemMoveDto>
    {
        public TaskItemMoveDtoValidator()
        {
            RuleFor(t => t.Id).RequiredGuid();
            RuleFor(t => t.ColumnId).RequiredGuid();
            RuleFor(t => t.LaneId).RequiredGuid();
            RuleFor(t => t.SortKey).NonNegativeSortKey();
            RuleFor(t => t.RowVersion).ConcurrencyTokenRules();
        }
    }
}
