using Application.Common.Validation.Extensions;
using Application.Lanes.DTOs;
using FluentValidation;

namespace Application.Lanes.Validation
{
    public sealed class LaneRenameDtoValidator : AbstractValidator<LaneRenameDto>
    {
        public LaneRenameDtoValidator()
        {
            RuleFor(l => l.NewName).LaneNameRules();
        }
    }
}
