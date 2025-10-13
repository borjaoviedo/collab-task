using Application.Common.Validation.Extensions;
using Application.Users.DTOs;
using FluentValidation;

namespace Application.Users.Validation
{
    public sealed class UserRenameDtoValidator : AbstractValidator<UserRenameDto>
    {
        public UserRenameDtoValidator()
        {
            RuleFor(u => u.NewName).UserNameRules();
        }
    }
}
