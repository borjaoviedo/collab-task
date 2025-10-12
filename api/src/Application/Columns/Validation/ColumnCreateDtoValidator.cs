using Application.Columns.DTOs;
using Application.Common.Validation.Extensions;
using FluentValidation;

namespace Application.Columns.Validation
{
    public sealed class ColumnCreateDtoValidator : AbstractValidator<ColumnCreateDto>
    {
        public ColumnCreateDtoValidator()
        {
            RuleFor(c => c.ProjectId).RequiredGuid();
            RuleFor(c => c.LaneId).RequiredGuid();
            RuleFor(c => c.Name).ColumnNameRules();
            RuleFor(c => c.Order).NonNegativeOrder();
        }
    }
}
