using Application.Columns.DTOs;
using Application.Common.Validation.Extensions;
using FluentValidation;

namespace Application.Columns.Validation
{
    public sealed class ColumnRenameDtoValidator : AbstractValidator<ColumnReadDto>
    {
        public ColumnRenameDtoValidator()
        {
            RuleFor(c => c.Id).RequiredGuid();
            RuleFor(c => c.Name).ColumnNameRules();
            RuleFor(c => c.RowVersion).ConcurrencyTokenRules();
        }
    }
}
