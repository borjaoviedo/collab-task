using Application.Common.Validation.Extensions;
using Application.Users.DTOs;
using FluentValidation;

namespace Application.Users.Validation
{
    public class UserRegisterDtoValidator : AbstractValidator<UserRegisterDto>
    {
        public UserRegisterDtoValidator()
        {
            RuleFor(u => u.Email).UserEmailRules();
            RuleFor(u => u.Name).UserNameRules();
            RuleFor(u => u.Password).UserPasswordRules();
        }
    }
}
