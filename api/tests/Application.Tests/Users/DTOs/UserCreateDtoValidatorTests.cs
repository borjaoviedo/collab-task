using Application.Users.DTOs;
using Application.Users.Validation;
using FluentValidation.TestHelper;

namespace Application.Tests.Users.DTOs
{
    public sealed class UserCreateDtoValidatorTests
    {
        private static string TooLongEmail()
        {
            var local = new string('a', 251);
            return $"{local}@x.com";
        }

        private readonly UserCreateDtoValidator _validator = new();

        [Fact]
        public void Email_Empty_Fails()
            => _validator.TestValidate(new UserCreateDto { Email = "", Password = "GoodPwd1!" })
                .ShouldHaveValidationErrorFor(x => x.Email);

        [Fact]
        public void Email_Invalid_Fails()
            => _validator.TestValidate(new UserCreateDto { Email = "bad", Password = "GoodPwd1!" })
                .ShouldHaveValidationErrorFor(x => x.Email);

        [Fact]
        public void Email_TooLong_Fails()
            => _validator.TestValidate(new UserCreateDto { Email = TooLongEmail(), Password = "GoodPwd1!" })
                .ShouldHaveValidationErrorFor(x => x.Email);

        [Theory]
        [InlineData("")]
        [InlineData("short1!")]
        [InlineData("alllower1!")]
        [InlineData("NoNumber!")]
        [InlineData("NoSpecial1")]
        public void Password_Invalid_Fails(string pwd)
            => _validator.TestValidate(new UserCreateDto { Email = "user@demo.com", Password = pwd })
                .ShouldHaveValidationErrorFor(x => x.Password);

        [Fact]
        public void Password_TooLong_Fails()
        {
            var pwd = new string('A', 257);
            _validator.TestValidate(new UserCreateDto { Email = "user@demo.com", Password = pwd })
                .ShouldHaveValidationErrorFor(x => x.Password);
        }

        [Fact]
        public void ValidDto_Passes()
            => _validator.TestValidate(new UserCreateDto { Email = "user@demo.com", Password = "GoodPwd1!" })
                .ShouldNotHaveAnyValidationErrors();
    }
}
