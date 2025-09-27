using Application.Common.Validation.Extensions;
using Application.Users.DTOs;
using FluentValidation;

namespace Application.Users.Validation
{
    public class UserCreateDtoValidator : AbstractValidator<UserCreateDto>
    {
        public UserCreateDtoValidator()
        {
            RuleFor(u => u.Email).UserEmailRules();
            RuleFor(u => u.Password).UserPasswordRules();
        }
    }
}
