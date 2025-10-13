using Application.Columns.DTOs;
using Application.Common.Validation.Extensions;
using FluentValidation;

namespace Application.Columns.Validation
{
    public sealed class ColumnRenameDtoValidator : AbstractValidator<ColumnRenameDto>
    {
        public ColumnRenameDtoValidator()
        {
            RuleFor(c => c.NewName).ColumnNameRules();
        }
    }
}
