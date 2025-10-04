using Application.Users.DTOs;
using Application.Users.Validation;
using FluentValidation.TestHelper;

namespace Application.Tests.Users.DTOs
{
    public sealed class UserLoginDtoValidatorTests
    {
        private static string TooLongEmail()
        {
            var local = new string('a', 251);
            return $"{local}@x.com";
        }

        private readonly UserLoginDtoValidator _validator = new();

        [Fact]
        public void Email_Empty_Fails()
            => _validator.TestValidate(new UserLoginDto { Email = "", Password = "GoodPwd1!" })
                .ShouldHaveValidationErrorFor(x => x.Email);

        [Fact]
        public void Email_Invalid_Fails()
            => _validator.TestValidate(new UserLoginDto { Email = "bad", Password = "GoodPwd1!" })
                .ShouldHaveValidationErrorFor(x => x.Email);

        [Fact]
        public void Email_TooLong_Fails()
            => _validator.TestValidate(new UserLoginDto { Email = TooLongEmail(), Password = "GoodPwd1!" })
                .ShouldHaveValidationErrorFor(x => x.Email);

        [Fact]
        public void Password_Empty_Fails()
            => _validator.TestValidate(new UserLoginDto { Email = "user@demo.com", Password = "" })
                .ShouldHaveValidationErrorFor(x => x.Password);

        [Fact]
        public void ValidDto_Passes()
            => _validator.TestValidate(new UserLoginDto { Email = "user@demo.com", Password = "GoodPwd1!" })
                .ShouldNotHaveAnyValidationErrors();
    }
}
