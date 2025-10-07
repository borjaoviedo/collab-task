using Application.Common.Validation.Extensions;
using Application.Projects.DTOs;
using FluentValidation;

namespace Application.Projects.Validation
{
    public sealed class ProjectUpdateDtoValidator : AbstractValidator<ProjectUpdateDto>
    {
        public ProjectUpdateDtoValidator()
        {
            RuleFor(x => x.RowVersion).ConcurrencyTokenRules();

            When(x => x.Name is not null, () =>
            {
                RuleFor(x => x.Name!).ProjectNameRules();
            });
        }
    }
}
