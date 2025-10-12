using Application.Common.Validation.Extensions;
using Application.Lanes.DTOs;
using FluentValidation;

namespace Application.Lanes.Validation
{
    public sealed class LaneCreateDtoValidator : AbstractValidator<LaneCreateDto>
    {
        public LaneCreateDtoValidator()
        {
            RuleFor(l => l.ProjectId).RequiredGuid();
            RuleFor(l => l.Name).LaneNameRules();
            RuleFor(l => l.Order).NonNegativeOrder(field: "Lane order");
        }
    }
}
