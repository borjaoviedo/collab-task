using Application.Columns.DTOs;
using Application.Common.Validation.Extensions;
using FluentValidation;

namespace Application.Columns.Validation
{
    public sealed class ColumnDeleteDtoValidator : AbstractValidator<ColumnDeleteDto>
    {
        public ColumnDeleteDtoValidator()
        {
            RuleFor(c => c.Id).RequiredGuid();
            RuleFor(c => c.RowVersion).ConcurrencyTokenRules();
        }
    }
}
