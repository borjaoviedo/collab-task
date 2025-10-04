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

        private static string TooLongName()
        {
            return new string('a', 101);
        }
        private readonly UserLoginDtoValidator _validator = new();

        [Fact]
        public void Email_Empty_Fails()
            => _validator.TestValidate(new UserLoginDto { Email = "", Name = "User Name", Password = "GoodPwd1!" })
                .ShouldHaveValidationErrorFor(x => x.Email);

        [Fact]
        public void Email_Invalid_Fails()
            => _validator.TestValidate(new UserLoginDto { Email = "bad", Name = "User Name", Password = "GoodPwd1!" })
                .ShouldHaveValidationErrorFor(x => x.Email);

        [Fact]
        public void Email_TooLong_Fails()
            => _validator.TestValidate(new UserLoginDto { Email = TooLongEmail(), Name = "User Name", Password = "GoodPwd1!" })
                .ShouldHaveValidationErrorFor(x => x.Email);

        [Fact]
        public void Name_Empty_Fails()
            => _validator.TestValidate(new UserLoginDto { Email = "user@demo.com", Name = "", Password = "GoodPwd1!" })
                .ShouldHaveValidationErrorFor(x => x.Name);

        [Fact]
        public void Name_Invalid_Fails()
            => _validator.TestValidate(new UserLoginDto { Email = "user@demo.com", Name = "User-Name", Password = "GoodPwd1!" })
                .ShouldHaveValidationErrorFor(x => x.Name);

        [Fact]
        public void Name_TooLong_Fails()
            => _validator.TestValidate(new UserLoginDto { Email = "user@demo.com", Name = TooLongName(), Password = "GoodPwd1!" })
                .ShouldHaveValidationErrorFor(x => x.Name);

        [Fact]
        public void Name_TooShort_Fails()
            => _validator.TestValidate(new UserLoginDto { Email = "user@demo.com", Name = "X", Password = "GoodPwd1!" })
                .ShouldHaveValidationErrorFor(x => x.Name);

        [Fact]
        public void Password_Empty_Fails()
            => _validator.TestValidate(new UserLoginDto { Email = "user@demo.com", Name = "User Name", Password = "" })
                .ShouldHaveValidationErrorFor(x => x.Password);

        [Fact]
        public void ValidDto_Passes()
            => _validator.TestValidate(new UserLoginDto { Email = "user@demo.com", Name = "User Name", Password = "GoodPwd1!" })
                .ShouldNotHaveAnyValidationErrors();
    }
}
