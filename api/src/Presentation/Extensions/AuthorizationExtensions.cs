using Api.Auth;
using Api.Auth.Authorization;
using Domain.Enums;
using Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace Api.Extensions
{
    public static class AuthorizationExtensions
    {
        public static IServiceCollection AddJwtAuth(this IServiceCollection services, IConfiguration cfg)
        {
            services.Configure<JwtOptions>(cfg.GetSection(JwtOptions.SectionName));

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
            {
                var jwt = cfg.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;
                o.TokenValidationParameters = new()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role
                };
            });

            return services;
        }

        public static IServiceCollection AddProjectAuthorization(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddSingleton<IAuthorizationHandler, ProjectRoleAuthorizationHandler>();

            services.AddAuthorization(options =>
            {
                options.AddPolicy(Policies.ProjectReader,
                    p => p.AddRequirements(new ProjectRoleRequirement(ProjectRole.Reader)));

                options.AddPolicy(Policies.ProjectMember,
                    p => p.AddRequirements(new ProjectRoleRequirement(ProjectRole.Member)));

                options.AddPolicy(Policies.ProjectAdmin,
                    p => p.AddRequirements(new ProjectRoleRequirement(ProjectRole.Admin)));

                options.AddPolicy(Policies.ProjectOwner,
                    p => p.AddRequirements(new ProjectRoleRequirement(ProjectRole.Owner)));
            });

            return services;
        }
    }
}
