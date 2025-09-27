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
            public string Password { get; set; } = "";
        }

        private sealed class DtoValidator : AbstractValidator<Dto>
        {
            public DtoValidator()
            {
                RuleFor(x => x.Email).UserEmailRules();
                RuleFor(x => x.Password).UserPasswordRules();
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
