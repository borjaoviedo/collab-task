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
            RuleFor(t => t.TargetColumnId).RequiredGuid();
            RuleFor(t => t.TargetLaneId).RequiredGuid();
            RuleFor(t => t.TargetSortKey).NonNegativeSortKey();
            RuleFor(t => t.RowVersion).ConcurrencyTokenRules();
        }
    }
}
