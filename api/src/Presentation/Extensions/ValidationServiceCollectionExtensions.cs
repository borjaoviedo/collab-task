using Application.Users.Validation;
using FluentValidation;

namespace Api.Extensions
{
    public static class ValidationServiceCollectionExtensions
    {
        public static IServiceCollection AddAppValidation(this IServiceCollection services)
        {
            services.AddValidatorsFromAssembly(typeof(UserCreateDtoValidator).Assembly);
            services.AddValidatorsFromAssembly(typeof(UserLoginDtoValidator).Assembly);
            return services;
        }
    }
}
