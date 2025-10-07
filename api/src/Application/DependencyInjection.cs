using Application.ProjectMembers.Abstractions;
using Application.ProjectMembers.Services;
using Application.Projects.Abstractions;
using Application.Projects.Services;
using Application.Users.Abstractions;
using Application.Users.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IProjectService, ProjectService>();
            services.AddScoped<IProjectMemberService, ProjectMemberService>();
            return services;
        }
    }
}
