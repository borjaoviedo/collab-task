using Application.Common.Validation.Extensions;
using Application.Lanes.DTOs;
using FluentValidation;

namespace Application.Lanes.Validation
{
    public sealed class LaneDeleteDtoValidator : AbstractValidator<LaneDeleteDto>
    {
        public LaneDeleteDtoValidator()
        {
            RuleFor(l => l.Id).RequiredGuid();
            RuleFor(l => l.RowVersion).ConcurrencyTokenRules();
        }
    }
}
