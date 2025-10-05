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
                .Matches(@"^[a-zA-Z\s]+$").WithMessage("User name must contain only letters.")
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

        public static IRuleBuilderOptions<T, string> ProjectNameRules<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder
                .NotEmpty().WithMessage("Project name is required.")
                .Must(s => !string.IsNullOrWhiteSpace(s)).WithMessage("Project name cannot be whitespace.")
                .MaximumLength(100).WithMessage("Project name length must be at most 100 characters.")
                .Must(name => !Regex.IsMatch(name, @"\s{2,}"))
                    .WithMessage("Project name cannot contain consecutive spaces.")
                .Must(s => s.All(c => !char.IsControl(c)))
                    .WithMessage("Project name contains invalid characters.");

        }

        public static IRuleBuilderOptions<T, byte[]> ConcurrencyTokenRules<T>(this IRuleBuilder<T, byte[]> ruleBuilder)
        {
            return ruleBuilder
                .NotNull().WithMessage("RowVersion is required.")
                .Must(v => v.Length > 0).WithMessage("RowVersion cannot be empty.");
        }

    }
}
