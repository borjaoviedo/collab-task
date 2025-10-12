using Application.Users.DTOs;
using Application.Users.Validation;
using FluentValidation.TestHelper;

namespace Application.Tests.Users.Validation
{
    public sealed class UserDtoValidatorTests
    {
        [Fact]
        public void Create_Invalid_Fails()
        {
            var v = new UserCreateDtoValidator();
            var dto = new UserCreateDto
            {
                Email = "bad",
                Name = "J0hn  Doe", // invalid chars + consecutive spaces
                Password = "weak"
            };
            var r = v.TestValidate(dto);
            r.ShouldHaveValidationErrorFor(x => x.Email).WithErrorMessage("Invalid email format.");
            r.ShouldHaveValidationErrorFor(x => x.Name).WithErrorMessage("User name must contain only letters.");
            r.ShouldHaveValidationErrorFor(x => x.Name).WithErrorMessage("User name cannot contain consecutive spaces.");
            r.ShouldHaveValidationErrorFor(x => x.Password); // any of the password rules
        }

        [Fact]
        public void Create_Name_Length_Bounds()
        {
            var v = new UserCreateDtoValidator();

            v.TestValidate(new UserCreateDto { Email = "a@b.com", Name = "A", Password = "GoodPwd1!" })
             .ShouldHaveValidationErrorFor(x => x.Name)
             .WithErrorMessage("User name must be at least 2 characters long.");

            v.TestValidate(new UserCreateDto { Email = "a@b.com", Name = new string('a', 101), Password = "GoodPwd1!" })
             .ShouldHaveValidationErrorFor(x => x.Name)
             .WithErrorMessage("User name length must be at most 100 characters.");
        }

        [Fact]
        public void Create_Email_TooLong_Fails()
        {
            var v = new UserCreateDtoValidator();
            var local = new string('a', 251);
            var dto = new UserCreateDto { Email = $"{local}@x.com", Name = "John", Password = "GoodPwd1!" };
            v.TestValidate(dto)
             .ShouldHaveValidationErrorFor(x => x.Email)
             .WithErrorMessage("Email length must be less than 256 characters.");
        }

        [Fact]
        public void Create_Valid_Passes()
        {
            var v = new UserCreateDtoValidator();
            var dto = new UserCreateDto
            {
                Email = "john@demo.com",
                Name = "John Doe",
                Password = "GoodPwd1!"
            };
            v.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData("")]
        [InlineData("short1!")]
        [InlineData("alllowercase1!")]
        [InlineData("NoNumber!")]
        [InlineData("NoSpecial1")]
        public void Login_Password_Invalid_Cases_Fail(string pwd)
        {
            var v = new UserLoginDtoValidator();
            v.TestValidate(new UserLoginDto { Email = "john@demo.com", Password = pwd })
             .ShouldHaveValidationErrorFor(x => x.Password);
        }

        [Fact]
        public void Login_Email_Invalid_Fails()
        {
            var v = new UserLoginDtoValidator();
            v.TestValidate(new UserLoginDto { Email = "not-an-email", Password = "GoodPwd1!" })
             .ShouldHaveValidationErrorFor(x => x.Email)
             .WithErrorMessage("Invalid email format.");
        }

        [Fact]
        public void Login_Valid_Passes()
        {
            var v = new UserLoginDtoValidator();
            v.TestValidate(new UserLoginDto { Email = "john@demo.com", Password = "GoodPwd1!" })
             .ShouldNotHaveAnyValidationErrors();
        }
    }
}
