using FluentValidation;
using System.Text.RegularExpressions;

namespace Application.Common.Validation.Extensions
{
    public static class RuleBuilderExtensions
    {
        public static IRuleBuilderOptions<T, string> UserEmailRules<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.")
                .MaximumLength(256).WithMessage("Email length must be less than 256 characters.");
        }

        public static IRuleBuilderOptions<T, string> UserNameRules<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder
                .NotEmpty().WithMessage("User name is required.")
                .MinimumLength(2).WithMessage("User name must be at least 2 characters long.")
                .MaximumLength(100).WithMessage("User name must not exceed 100 characters.")
                .Matches(@"^[a-zA-Z\s]+$").WithMessage("User name must contain only letters and single spaces.")
                .Must(name => !Regex.IsMatch(name, @"\s{2,}"))
                    .WithMessage("User name cannot contain consecutive spaces.");
        }

        public static IRuleBuilderOptions<T, string> UserPasswordRules<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(8).WithMessage("Password must have at least 8 characters.")
                .MaximumLength(256).WithMessage("Password length must be less than 256 characters.")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches("[0-9]").WithMessage("Password must contain at least one number.")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
        }
    }
}
