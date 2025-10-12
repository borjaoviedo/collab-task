using Application.Common.Validation.Extensions;
using Application.Users.DTOs;
using FluentValidation;

namespace Application.Users.Validation
{
    public sealed class UserChangeRoleDtoValidator : AbstractValidator<UserChangeRoleDto>
    {
        public UserChangeRoleDtoValidator()
        {
            RuleFor(u => u.NewRole).UserRoleRules();
        }
    }
}
