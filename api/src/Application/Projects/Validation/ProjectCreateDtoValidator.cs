using Application.Common.Validation.Extensions;
using Application.Users.DTOs;
using FluentValidation;

namespace Application.Projects.Validation
{
    public class ProjectCreateDtoValidator : AbstractValidator<UserCreateDto>
    {
        public ProjectCreateDtoValidator()
        {
            RuleFor(p => p.Name).ProjectNameRules();
        }
    }
}
