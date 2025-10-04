using Api.Auth.Authorization;
using Api.Auth.Services;
using Application.Common.Abstractions.Auth;
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
        public static IServiceCollection AddJwtAuthAndPolicies(this IServiceCollection services, IConfiguration cfg)
        {
            services.Configure<JwtOptions>(cfg.GetSection(JwtOptions.SectionName));

            services.AddHttpContextAccessor()
                .AddScoped<ICurrentUserService, CurrentUserService>()
                .AddProjectAuthorization()
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
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

        private static IServiceCollection AddProjectAuthorization(this IServiceCollection services)
        {
            services.AddScoped<IAuthorizationHandler, ProjectRoleAuthorizationHandler>()
                .AddAuthorizationBuilder()
                .AddPolicy(Policies.ProjectReader, p => p.AddRequirements(new ProjectRoleRequirement(ProjectRole.Reader)))
                .AddPolicy(Policies.ProjectMember, p => p.AddRequirements(new ProjectRoleRequirement(ProjectRole.Member)))
                .AddPolicy(Policies.ProjectAdmin, p => p.AddRequirements(new ProjectRoleRequirement(ProjectRole.Admin)))
                .AddPolicy(Policies.ProjectOwner, p => p.AddRequirements(new ProjectRoleRequirement(ProjectRole.Owner)));

            return services;
        }
    }
}
