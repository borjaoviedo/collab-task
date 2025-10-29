using Application.Users.DTOs;
using Application.Users.Validation;
using Domain.Enums;
using FluentValidation.TestHelper;

namespace Application.Tests.Users.Validation
{
    public sealed class UserDtoValidatorTests
    {
        [Fact]
        public void Create_Invalid_Fails()
        {
            var validator = new UserRegisterDtoValidator();
            var dto = new UserRegisterDto
            {
                Email = "bad",
                Name = "J0hn  Doe", // invalid chars + consecutive spaces
                Password = "weak"
            };
            var validationResult = validator.TestValidate(dto);
            validationResult
                .ShouldHaveValidationErrorFor(u => u.Email)
                .WithErrorMessage("Invalid email format.");
            validationResult
                .ShouldHaveValidationErrorFor(u => u.Name)
                .WithErrorMessage("User name must contain only letters.");
            validationResult
                .ShouldHaveValidationErrorFor(u => u.Name)
                .WithErrorMessage("User name cannot contain consecutive spaces.");
            validationResult
                .ShouldHaveValidationErrorFor(u => u.Password); // any of the password rules
        }

        [Fact]
        public void Create_Name_Length_Bounds()
        {
            var validator = new UserRegisterDtoValidator();

            var dto1 = new UserRegisterDto { Email = "a@b.com", Name = "A", Password = "GoodPwd1!" };
            validator.TestValidate(dto1)
             .ShouldHaveValidationErrorFor(u => u.Name)
             .WithErrorMessage("User name must be at least 2 characters long.");

            var dto2 = new UserRegisterDto { Email = "a@b.com", Name = new string('a', 101), Password = "GoodPwd1!" };
            validator.TestValidate(dto2)
             .ShouldHaveValidationErrorFor(u => u.Name)
             .WithErrorMessage("User name length must be at most 100 characters.");
        }

        [Fact]
        public void Create_Email_TooLong_Fails()
        {
            var validator = new UserRegisterDtoValidator();
            var local = new string('a', 251);
            var dto = new UserRegisterDto { Email = $"{local}@x.com", Name = "John", Password = "GoodPwd1!" };

            validator.TestValidate(dto)
             .ShouldHaveValidationErrorFor(u => u.Email)
             .WithErrorMessage("Email length must be less than 256 characters.");
        }

        [Fact]
        public void Create_Valid_Passes()
        {
            var validator = new UserRegisterDtoValidator();
            var dto = new UserRegisterDto
            {
                Email = "john@demo.com",
                Name = "John Doe",
                Password = "GoodPwd1!"
            };
            validator.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData("")]
        [InlineData("short1!")]
        [InlineData("alllowercase1!")]
        [InlineData("NoNumber!")]
        [InlineData("NoSpecial1")]
        public void Login_Password_Invalid_Cases_Fail(string pwd)
        {
            var validator = new UserLoginDtoValidator();
            validator.TestValidate(new UserLoginDto { Email = "john@demo.com", Password = pwd })
             .ShouldHaveValidationErrorFor(u => u.Password);
        }

        [Fact]
        public void Login_Email_Invalid_Fails()
        {
            var validator = new UserLoginDtoValidator();
            validator.TestValidate(new UserLoginDto { Email = "not-an-email", Password = "GoodPwd1!" })
             .ShouldHaveValidationErrorFor(u => u.Email)
             .WithErrorMessage("Invalid email format.");
        }

        [Fact]
        public void Login_Valid_Passes()
        {
            var validator = new UserLoginDtoValidator();
            validator.TestValidate(new UserLoginDto { Email = "john@demo.com", Password = "GoodPwd1!" })
             .ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Rename_Valid_Passes()
        {
            var validator = new UserRenameDtoValidator();
            validator.TestValidate(new UserRenameDto { NewName = "Valid" })
                .ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Rename_Invalid_Fails()
        {
            var validator = new UserRenameDtoValidator();
            validator.TestValidate(new UserRenameDto { NewName = $"{Guid.NewGuid()}" })
                .ShouldHaveValidationErrorFor(u => u.NewName)
                .WithErrorMessage("User name must contain only letters.");

            validator.TestValidate(new UserRenameDto { NewName = "x" })
                .ShouldHaveValidationErrorFor(u => u.NewName)
                .WithErrorMessage("User name must be at least 2 characters long.");

            validator.TestValidate(new UserRenameDto { NewName = new string('x', 101)})
                .ShouldHaveValidationErrorFor(u => u.NewName)
                .WithErrorMessage("User name length must be at most 100 characters.");
        }

        [Fact]
        public void ChangeRole_Valid_Passes()
        {
            var validator = new UserChangeRoleDtoValidator();
            validator.TestValidate(new UserChangeRoleDto { NewRole = UserRole.User })
                .ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void ChangeRole_Invalid_Fails()
        {
            var validator = new UserChangeRoleDtoValidator();
            validator.TestValidate(new UserChangeRoleDto { NewRole = (UserRole)2 })
                .ShouldHaveValidationErrorFor(u => u.NewRole)
                .WithErrorMessage("Invalid user role value.");
        }
    }
}
