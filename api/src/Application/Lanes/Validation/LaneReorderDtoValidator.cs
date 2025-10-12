using Application.Common.Validation.Extensions;
using Application.Lanes.DTOs;
using FluentValidation;

namespace Application.Lanes.Validation
{
    public sealed class LaneReorderDtoValidator :  AbstractValidator<LaneReorderDto>
    {
        public LaneReorderDtoValidator()
        {
            RuleFor(l => l.Id).RequiredGuid();
            RuleFor(l => l.NewOrder).NonNegativeOrder(field: "Lane order");
            RuleFor(l => l.RowVersion).ConcurrencyTokenRules();
        }
    }
}
