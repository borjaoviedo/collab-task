using Application.Common.Validation.Extensions;
using FluentValidation;
using FluentValidation.TestHelper;

namespace Application.Tests.Common.Validation.Extensions
{
    public sealed class RuleBuilderExtensionsTests
    {
        private sealed class Dto
        {
            public string Email { get; set; } = "";
            public string Name { get; set; } = "";
            public string Password { get; set; } = "";
        }

        private sealed class DtoValidator : AbstractValidator<Dto>
        {
            public DtoValidator()
            {
                RuleFor(x => x.Email).UserEmailRules();
                RuleFor(x => x.Password).UserPasswordRules();
                RuleFor(x => x.Name).UserNameRules();
            }
        }

        private readonly DtoValidator _validator = new();

        [Fact]
        public void Email_Empty_Fails()
            => _validator.TestValidate(new Dto { Email = "" })
                .ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Email is required.");

        [Fact]
        public void Email_InvalidFormat_Fails()
            => _validator.TestValidate(new Dto { Email = "not-an-email" })
                .ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Invalid email format.");

        [Fact]
        public void Email_TooLong_Fails()
        {
            var local = new string('a', 251);
            var dto = new Dto { Email = $"{local}@x.com" };
            _validator.TestValidate(dto)
                .ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Email length must be less than 256 characters.");
        }

        [Fact]
        public void Name_Empty_Fails()
            => _validator.TestValidate(new Dto { Name = "" })
                .ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("User name is required.");

        [Fact]
        public void Name_TooShort_Fails()
        {
            var dto = new Dto { Name = "z" };
            _validator.TestValidate(dto)
                .ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("User name must be at least 2 characters long.");
        }

        [Fact]
        public void Name_TooLong_Fails()
        {
            var dto = new Dto { Name = new string('a', 101) };
            _validator.TestValidate(dto)
                .ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("User name must not exceed 100 characters.");
        }

        [Theory]
        [InlineData("John D.")]
        [InlineData("John D0e")]
        public void Name_InvalidFormat_Fails(string input)
            => _validator.TestValidate(new Dto { Name = input })
                .ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("User name must contain only letters.");

        [Fact]
        public void Name_With_Two_Consecutive_Spaces_Fails()
        {
            var dto = new Dto { Name = "John  Doe" };
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
            => _validator.TestValidate(new Dto { Password = pwd })
                .ShouldHaveValidationErrorFor(x => x.Password);

        [Fact]
        public void Password_TooLong_Fails()
        {
            var pwd = new string('A', 257);
            _validator.TestValidate(new Dto { Password = pwd })
                .ShouldHaveValidationErrorFor(x => x.Password)
                .WithErrorMessage("Password length must be less than 256 characters.");
        }

        [Fact]
        public void Password_Valid_Passes()
            => _validator.TestValidate(new Dto { Password = "GoodPwd1!" })
                .ShouldNotHaveValidationErrorFor(x => x.Password);
    }
}
