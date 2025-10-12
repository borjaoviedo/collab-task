using Domain.Enums;
using FluentValidation;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Application.Common.Validation.Extensions
{
    public static class RuleBuilderExtensions
    {
        private static readonly Regex TwoOrMoreSpaces = new(@"\s{2,}", RegexOptions.Compiled);

        // Helpers
        private static IRuleBuilderOptions<T, string> CommonNameRules<T>(
            IRuleBuilder<T, string> rb, int maxLen, string field) =>
            rb.NotEmpty().WithMessage($"{field} is required.")
              .Must(s => !string.IsNullOrWhiteSpace(s)).WithMessage($"{field} cannot be whitespace.")
              .Must(s => s!.Trim().Length <= maxLen).WithMessage($"{field} length must be at most {maxLen} characters.")
              .Must(s => !TwoOrMoreSpaces.IsMatch(s!.Trim()))
                .WithMessage($"{field} cannot contain consecutive spaces.");

        private static bool IsUtc(DateTimeOffset d) => d.Offset == TimeSpan.Zero;

        private static bool IsValidJson(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            try { using var _ = JsonDocument.Parse(s); return true; }
            catch (JsonException) { return false; }
        }

        private static bool BeNullOrFutureUtc(DateTimeOffset? due)
        {
            if (due is null) return true;
            if (due.Value.Offset != TimeSpan.Zero) return false;
            return due.Value >= DateTimeOffset.UtcNow;
        }

        // User
        public static IRuleBuilderOptions<T, string> UserEmailRules<T>(this IRuleBuilder<T, string> rb) =>
            rb.NotEmpty().WithMessage("Email is required.")
              .EmailAddress().WithMessage("Invalid email format.")
              .Must(s => s!.Trim().Length <= 256).WithMessage("Email length must be less than 256 characters.");

        public static IRuleBuilderOptions<T, string> UserNameRules<T>(this IRuleBuilder<T, string> rb) =>
            CommonNameRules(rb, 100, "User name")
              .MinimumLength(2).WithMessage("User name must be at least 2 characters long.")
              .Matches(@"^[a-zA-Z\s]+$").WithMessage("User name must contain only letters.");

        public static IRuleBuilderOptions<T, string> UserPasswordRules<T>(this IRuleBuilder<T, string> rb) =>
            rb.NotEmpty().WithMessage("Password is required.")
              .MinimumLength(8).WithMessage("Password must have at least 8 characters.")
              .MaximumLength(256).WithMessage("Password length must be less than 256 characters.")
              .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
              .Matches("[0-9]").WithMessage("Password must contain at least one number.")
              .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

        // Project
        public static IRuleBuilderOptions<T, string> ProjectNameRules<T>(this IRuleBuilder<T, string> rb) =>
            CommonNameRules(rb, 100, "Project name")
              .Must(s => s!.Trim().All(c => !char.IsControl(c)))
              .WithMessage("Project name contains invalid characters.");

        // Concurrency
        public static IRuleBuilderOptions<T, byte[]> ConcurrencyTokenRules<T>(this IRuleBuilder<T, byte[]> rb) =>
            rb.NotNull().WithMessage("RowVersion is required.")
              .Must(v => v.Length > 0).WithMessage("RowVersion cannot be empty.");

        // IDs and enums
        public static IRuleBuilderOptions<T, Guid> RequiredGuid<T>(this IRuleBuilder<T, Guid> rb) =>
            rb.NotEmpty().WithMessage("Id is required.");

        public static IRuleBuilderOptions<T, ProjectRole> ProjectRoleRules<T>(this IRuleBuilder<T, ProjectRole> rb) =>
            rb.Must(r => Enum.IsDefined(typeof(ProjectRole), r)).WithMessage("Invalid project role value.");

        public static IRuleBuilderOptions<T, UserRole> UserRoleRules<T>(this IRuleBuilder<T, UserRole> rb) =>
            rb.Must(r => Enum.IsDefined(typeof(UserRole), r)).WithMessage("Invalid user role value.");

        public static IRuleBuilderOptions<T, TaskRole> TaskRoleRules<T>(this IRuleBuilder<T, TaskRole> rb) =>
            rb.Must(r => Enum.IsDefined(typeof(TaskRole), r)).WithMessage("Invalid task role value.");

        public static IRuleBuilderOptions<T, TaskActivityType> TaskActivityTypeRules<T>(this IRuleBuilder<T, TaskActivityType> rb) =>
            rb.Must(r => Enum.IsDefined(typeof(TaskActivityType), r)).WithMessage("Invalid task activity type value.");

        // Dates
        public static IRuleBuilderOptions<T, DateTimeOffset> JoinedAtRules<T>(this IRuleBuilder<T, DateTimeOffset> rb) =>
            rb.Must(IsUtc).WithMessage("JoinedAt must be in UTC.")
              .LessThanOrEqualTo(_ => DateTimeOffset.UtcNow).WithMessage("JoinedAt cannot be in the future.")
              .GreaterThan(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero)).WithMessage("JoinedAt is too old.");

        public static IRuleBuilderOptions<T, DateTimeOffset?> RemovedAtRules<T>(this IRuleBuilder<T, DateTimeOffset?> rb) =>
            rb.Must(d => d is null || IsUtc(d.Value)).WithMessage("RemovedAt must be in UTC.")
              .Must(d => d is null || d <= DateTimeOffset.UtcNow).WithMessage("RemovedAt cannot be in the future.");

        public static IRuleBuilderOptions<T, DateTimeOffset?> DueDateRules<T>(this IRuleBuilder<T, DateTimeOffset?> rb)
            => rb.Must(BeNullOrFutureUtc).WithMessage("DueDate must be null or a UTC date/time in the future.");

        // Board names
        public static IRuleBuilderOptions<T, string> ColumnNameRules<T>(this IRuleBuilder<T, string> rb) =>
            CommonNameRules(rb, 100, "Column name");

        public static IRuleBuilderOptions<T, string> LaneNameRules<T>(this IRuleBuilder<T, string> rb) =>
            CommonNameRules(rb, 100, "Lane name");

        public static IRuleBuilderOptions<T, string> TaskTitleRules<T>(this IRuleBuilder<T, string> rb) =>
            CommonNameRules(rb, 100, "Task title");

        public static IRuleBuilderOptions<T, string> TaskDescriptionRules<T>(this IRuleBuilder<T, string> rb) =>
            rb.NotEmpty().WithMessage("Task description is required.")
              .Must(s => s!.Trim().Length <= 2000)
              .WithMessage("Task description length must be at most 2000 characters.");

        public static IRuleBuilderOptions<T, string> NoteContentRules<T>(this IRuleBuilder<T, string> rb) =>
            rb.NotEmpty().WithMessage("Note content is required.")
              .Must(s => s!.Trim().Length <= 500)
              .WithMessage("Note content length must be at most 500 characters.");

        public static IRuleBuilderOptions<T, string> ActivityPayloadRules<T>(this IRuleBuilder<T, string> rb) =>
            rb.NotEmpty().WithMessage("Activity payload is required.")
              .Must(s => !string.IsNullOrWhiteSpace(s)).WithMessage("Activity payload cannot be whitespace.")
              .Must(IsValidJson).WithMessage("Activity payload must be valid JSON.");

        // Orders and SortKeys
        public static IRuleBuilderOptions<T, int> NonNegativeOrder<T>(this IRuleBuilder<T, int> rb, string field = "Order") =>
            rb.GreaterThanOrEqualTo(0).WithMessage($"{field} must be ≥ 0.");

        public static IRuleBuilderOptions<T, decimal> NonNegativeSortKey<T>(this IRuleBuilder<T, decimal> rb) =>
            rb.GreaterThanOrEqualTo(0m).WithMessage("SortKey must be ≥ 0.");
    }
}
