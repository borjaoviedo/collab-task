using Application.Users.DTOs;
using Domain.Enums;

namespace TestHelpers.Api.Endpoints.Defaults
{
    public static class UserDefaults
    {
        public readonly static string DefaultEmail = $"{Guid.NewGuid():N}@demo.com";
        public readonly static string DefaultUserName = "User name";
        public readonly static string DefaultPassword = "Str0ngP@ss!";
        public readonly static string DefaultUserRename = "different name";
        public readonly static UserRole DefaultUserChangeRole = UserRole.Admin;

        public readonly static UserRegisterDto DefaultUserRegisterDto = new()
        {
            Email = DefaultEmail,
            Name = DefaultUserName,
            Password = DefaultPassword
        };

        public readonly static UserLoginDto DefaultUserLoginDto = new()
        {
            Email = DefaultEmail,
            Password = DefaultPassword
        };

        public readonly static UserRenameDto DefaultUserRenameDto = new() { NewName = DefaultUserRename };

        public readonly static UserChangeRoleDto DefaultUserChangeRoleDto = new() { NewRole = DefaultUserChangeRole };
    }
}
