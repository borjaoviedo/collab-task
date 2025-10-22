using Application.Common.Validation.Extensions;
using Domain.Enums;
using FluentValidation;
using FluentValidation.TestHelper;
using System.Text;

namespace Application.Tests.Common.Validation.Extensions
{
    public sealed class RuleBuilderExtensionsTests
    {
        private sealed class UserDto
        {
            public string Email { get; set; } = "";
            public string Name { get; set; } = "";
            public string Password { get; set; } = "";
        }

        private sealed class UserDtoValidator : AbstractValidator<UserDto>
        {
            public UserDtoValidator()
            {
                RuleFor(x => x.Email).UserEmailRules();
                RuleFor(x => x.Password).UserPasswordRules();
                RuleFor(x => x.Name).UserNameRules();
            }
        }

        private readonly UserDtoValidator _validator = new();

        [Fact]
        public void Email_Empty_Fails()
            => _validator.TestValidate(new UserDto { Email = "" })
                .ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Email is required.");

        [Fact]
        public void Email_InvalidFormat_Fails()
            => _validator.TestValidate(new UserDto { Email = "not-an-email" })
                .ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Invalid email format.");

        [Fact]
        public void Email_TooLong_Fails()
        {
            var local = new string('a', 251);
            var dto = new UserDto { Email = $"{local}@x.com" };
            _validator.TestValidate(dto)
                .ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Email length must be less than 256 characters.");
        }

        [Fact]
        public void Name_Empty_Fails()
            => _validator.TestValidate(new UserDto { Name = "" })
                .ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("User name is required.");

        [Fact]
        public void Name_TooShort_Fails()
        {
            var dto = new UserDto { Name = "z" };
            _validator.TestValidate(dto)
                .ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("User name must be at least 2 characters long.");
        }

        [Fact]
        public void Name_TooLong_Fails()
        {
            var dto = new UserDto { Name = new string('a', 101) };
            _validator.TestValidate(dto)
                .ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("User name length must be at most 100 characters.");
        }

        [Theory]
        [InlineData("John D.")]
        [InlineData("John D0e")]
        public void Name_InvalidFormat_Fails(string input)
            => _validator.TestValidate(new UserDto { Name = input })
                .ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("User name must contain only letters.");

        [Fact]
        public void Name_With_Two_Consecutive_Spaces_Fails()
        {
            var dto = new UserDto { Name = "John  Doe" };
            _validator.TestValidate(dto)
                .ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("User name cannot contain consecutive spaces.");
        }

        [Theory]
        [InlineData("")]
        [InlineData("short1!")]
        [InlineData("alllowercase1!")]
        [InlineData("NoNumber!")]
        [InlineData("NoSpecial1")]
        public void Password_Invalid_Cases_Fail(string pwd)
            => _validator.TestValidate(new UserDto { Password = pwd })
                .ShouldHaveValidationErrorFor(x => x.Password);

        [Fact]
        public void Password_TooLong_Fails()
        {
            var pwd = new string('A', 257);
            _validator.TestValidate(new UserDto { Password = pwd })
                .ShouldHaveValidationErrorFor(x => x.Password)
                .WithErrorMessage("Password length must be less than 256 characters.");
        }

        [Fact]
        public void Password_Valid_Passes()
            => _validator.TestValidate(new UserDto { Password = "GoodPwd1!" })
                .ShouldNotHaveValidationErrorFor(x => x.Password);

        private sealed class IdsEnumsDto
        {
            public Guid ProjectId { get; set; }
            public ProjectRole ProjectRole { get; set; }
            public UserRole UserRole { get; set; }
            public TaskRole TaskRole { get; set; }
            public TaskActivityType ActivityType { get; set; }
        }

        private sealed class IdsEnumsValidator : AbstractValidator<IdsEnumsDto>
        {
            public IdsEnumsValidator()
            {
                RuleFor(x => x.ProjectId).RequiredGuid();
                RuleFor(x => x.ProjectRole).ProjectRoleRules();
                RuleFor(x => x.UserRole).UserRoleRules();
                RuleFor(x => x.TaskRole).TaskRoleRules();
                RuleFor(x => x.ActivityType).TaskActivityTypeRules();
            }
        }

        [Fact]
        public void RequiredGuid_Empty_Fails()
        {
            var v = new IdsEnumsValidator();
            v.TestValidate(new IdsEnumsDto { ProjectId = Guid.Empty })
             .ShouldHaveValidationErrorFor(x => x.ProjectId)
             .WithErrorMessage("Id is required.");
        }

        [Fact]
        public void Enums_Invalid_Fail()
        {
            var v = new IdsEnumsValidator();
            v.TestValidate(new IdsEnumsDto
            {
                ProjectId = Guid.NewGuid(),
                ProjectRole = (ProjectRole)999,
                UserRole = (UserRole)999,
                TaskRole = (TaskRole)999,
                ActivityType = (TaskActivityType)999
            })
            .ShouldHaveValidationErrors();
        }

        [Fact]
        public void Enums_Valid_Pass()
        {
            var v = new IdsEnumsValidator();
            v.TestValidate(new IdsEnumsDto
            {
                ProjectId = Guid.NewGuid(),
                ProjectRole = ProjectRole.Member,
                UserRole = UserRole.User,
                TaskRole = TaskRole.Owner,
                ActivityType = TaskActivityType.TaskCreated
            })
            .ShouldNotHaveAnyValidationErrors();
        }

        private sealed class DatesDto
        {
            public DateTimeOffset? DueDate { get; set; }
        }

        private sealed class DatesValidator : AbstractValidator<DatesDto>
        {
            public DatesValidator()
            {
                RuleFor(x => x.DueDate).DueDateRules();
            }
        }

        [Theory]
        [InlineData(null)]
        public void DueDate_Null_Passes(DateTimeOffset? d)
        {
            var v = new DatesValidator();
            v.TestValidate(new DatesDto { DueDate = d })
             .ShouldNotHaveValidationErrorFor(x => x.DueDate);
        }

        [Fact]
        public void DueDate_Past_Fails()
        {
            var v = new DatesValidator();
            var past = DateTimeOffset.UtcNow.AddDays(-1);
            v.TestValidate(new DatesDto { DueDate = past })
             .ShouldHaveValidationErrorFor(x => x.DueDate)
             .WithErrorMessage("DueDate must be null or a UTC date/time in the future.");
        }

        [Fact]
        public void DueDate_NotUtc_Fails()
        {
            var v = new DatesValidator();
            var local = new DateTimeOffset(2025, 10, 12, 12, 0, 0, TimeSpan.FromHours(+2));
            v.TestValidate(new DatesDto { DueDate = local })
             .ShouldHaveValidationErrorFor(x => x.DueDate);
        }

        [Fact]
        public void DueDate_FutureUtc_Passes()
        {
            var v = new DatesValidator();
            var future = DateTimeOffset.UtcNow.AddDays(1);
            v.TestValidate(new DatesDto { DueDate = future })
             .ShouldNotHaveValidationErrorFor(x => x.DueDate);
        }


        private sealed class BoardNamesDto
        {
            public string ColumnName { get; set; } = "";
            public string LaneName { get; set; } = "";
            public string TaskTitle { get; set; } = "";
            public string TaskDescription { get; set; } = "";
            public string NoteContent { get; set; } = "";
        }

        private sealed class BoardNamesValidator : AbstractValidator<BoardNamesDto>
        {
            public BoardNamesValidator()
            {
                RuleFor(x => x.ColumnName).ColumnNameRules();
                RuleFor(x => x.LaneName).LaneNameRules();
                RuleFor(x => x.TaskTitle).TaskTitleRules();
                RuleFor(x => x.TaskDescription).TaskDescriptionRules();
                RuleFor(x => x.NoteContent).NoteContentRules();
            }
        }

        [Fact]
        public void ColumnName_Whitespace_Fails()
        {
            var v = new BoardNamesValidator();
            v.TestValidate(new BoardNamesDto { ColumnName = "   " })
             .ShouldHaveValidationErrorFor(x => x.ColumnName)
             .WithErrorMessage("Column name cannot be whitespace.");
        }

        [Fact]
        public void LaneName_ConsecutiveSpaces_Fails()
        {
            var v = new BoardNamesValidator();
            v.TestValidate(new BoardNamesDto { LaneName = "Doing  Now" })
             .ShouldHaveValidationErrorFor(x => x.LaneName)
             .WithErrorMessage("Lane name cannot contain consecutive spaces.");
        }

        [Fact]
        public void TaskTitle_TooLong_Fails()
        {
            var v = new BoardNamesValidator();
            v.TestValidate(new BoardNamesDto { TaskTitle = new string('x', 101) })
             .ShouldHaveValidationErrorFor(x => x.TaskTitle)
             .WithErrorMessage("Task title length must be at most 100 characters.");
        }

        [Fact]
        public void TaskDescription_Empty_Fails()
        {
            var v = new BoardNamesValidator();
            v.TestValidate(new BoardNamesDto { TaskDescription = "" })
             .ShouldHaveValidationErrorFor(x => x.TaskDescription)
             .WithErrorMessage("Task description is required.");
        }

        [Fact]
        public void TaskDescription_TooLong_Fails()
        {
            var v = new BoardNamesValidator();
            var tooLong = new string('y', 2001);
            v.TestValidate(new BoardNamesDto { TaskDescription = tooLong })
             .ShouldHaveValidationErrorFor(x => x.TaskDescription)
             .WithErrorMessage("Task description length must be at most 2000 characters.");
        }

        [Fact]
        public void NoteContent_Length_TooLong_Fails()
        {
            var v = new BoardNamesValidator();
            var tooLong = new string('n', 501);
            v.TestValidate(new BoardNamesDto { NoteContent = tooLong })
             .ShouldHaveValidationErrorFor(x => x.NoteContent)
             .WithErrorMessage("Note content length must be at most 500 characters.");
        }

        [Fact]
        public void BoardNames_Valid_Pass()
        {
            var v = new BoardNamesValidator();
            v.TestValidate(new BoardNamesDto
            {
                ColumnName = "To Do",
                LaneName = "Backlog",
                TaskTitle = "Short title",
                TaskDescription = "Some description",
                NoteContent = "Note"
            })
            .ShouldNotHaveAnyValidationErrors();
        }

        private sealed class ConcurrencyDto
        {
            public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        }

        private sealed class ConcurrencyValidator : AbstractValidator<ConcurrencyDto>
        {
            public ConcurrencyValidator()
            {
                RuleFor(x => x.RowVersion).ConcurrencyTokenRules();
            }
        }

        [Fact]
        public void ConcurrencyToken_Empty_Fails()
        {
            var v = new ConcurrencyValidator();
            v.TestValidate(new ConcurrencyDto { RowVersion = Array.Empty<byte>() })
             .ShouldHaveValidationErrorFor(x => x.RowVersion)
             .WithErrorMessage("RowVersion cannot be empty.");
        }

        [Fact]
        public void ConcurrencyToken_NonEmpty_Passes()
        {
            var v = new ConcurrencyValidator();
            v.TestValidate(new ConcurrencyDto { RowVersion = Encoding.UTF8.GetBytes("rv") })
             .ShouldNotHaveValidationErrorFor(x => x.RowVersion);
        }

        private sealed class OrderDto
        {
            public int Order { get; set; }
        }

        private sealed class OrderValidator : AbstractValidator<OrderDto>
        {
            public OrderValidator()
            {
                RuleFor(x => x.Order).NonNegativeOrder();
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        [InlineData(9999)]
        public void Order_Valid_Passes(int value)
        {
            var v = new OrderValidator();
            v.TestValidate(new OrderDto { Order = value })
             .ShouldNotHaveValidationErrorFor(x => x.Order);
        }

        [Fact]
        public void Order_Negative_Fails()
        {
            var v = new OrderValidator();
            v.TestValidate(new OrderDto { Order = -1 })
             .ShouldHaveValidationErrorFor(x => x.Order)
             .WithErrorMessage("Order must be ≥ 0.");
        }

        private sealed class SortKeyDto
        {
            public decimal SortKey { get; set; }
        }

        private sealed class SortKeyValidator : AbstractValidator<SortKeyDto>
        {
            public SortKeyValidator()
            {
                RuleFor(x => x.SortKey).NonNegativeSortKey();
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(0.1)]
        [InlineData(123.456)]
        public void SortKey_Valid_Passes(decimal value)
        {
            var v = new SortKeyValidator();
            v.TestValidate(new SortKeyDto { SortKey = value })
             .ShouldNotHaveValidationErrorFor(x => x.SortKey);
        }

        [Fact]
        public void SortKey_Negative_Fails()
        {
            var v = new SortKeyValidator();
            v.TestValidate(new SortKeyDto { SortKey = -0.01m })
             .ShouldHaveValidationErrorFor(x => x.SortKey)
             .WithErrorMessage("SortKey must be ≥ 0.");
        }
    }
}
