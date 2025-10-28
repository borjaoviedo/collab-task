using Application.Users.DTOs;

namespace TestHelpers.Api.Defaults
{
    public static class UserDefaults
    {
        public readonly static string DefaultEmail = $"{Guid.NewGuid():N}@demo.com";
        public readonly static string DefaultUserName = "User name";
        public readonly static string DefaultPassword = "Str0ngP@ss!";

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
    }
}
