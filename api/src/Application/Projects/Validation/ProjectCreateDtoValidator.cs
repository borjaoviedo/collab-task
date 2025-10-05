using Application.Common.Validation.Extensions;
using Application.Projects.DTOs;
using FluentValidation;

namespace Application.Projects.Validation
{
    public class ProjectCreateDtoValidator : AbstractValidator<ProjectCreateDto>
    {
        public ProjectCreateDtoValidator()
        {
            RuleFor(p => p.Name).ProjectNameRules();
        }
    }
}
