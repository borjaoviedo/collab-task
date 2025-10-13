using Application.Columns.DTOs;
using Application.Common.Validation.Extensions;
using FluentValidation;

namespace Application.Columns.Validation
{
    public sealed class ColumnReorderDtoValidator : AbstractValidator<ColumnReorderDto>
    {
        public ColumnReorderDtoValidator()
        {
            RuleFor(c => c.NewOrder).NonNegativeOrder(field: "Column order");
        }
    }
}
