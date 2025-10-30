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
    /// <summary>
    /// Service collection extensions for configuring JWT authentication and application authorization policies.
    /// Registers JWT bearer validation, current-user context service, and domain-based authorization handlers
    /// for both project-level and system-level roles.
    /// </summary>
    public static class AuthorizationExtensions
    {
        /// <summary>
        /// Configures JWT authentication and all authorization policies.
        /// Loads <see cref="JwtOptions"/> from configuration, sets token validation parameters,
        /// and registers project/system role-based requirements.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <param name="cfg">Application configuration containing JWT settings.</param>
        /// <returns>The same service collection for chaining.</returns>
        public static IServiceCollection AddJwtAuthAndPolicies(this IServiceCollection services, IConfiguration cfg)
        {
            services.Configure<JwtOptions>(cfg.GetSection(JwtOptions.SectionName));

            services.AddHttpContextAccessor()
                .AddScoped<ICurrentUserService, CurrentUserService>()
                .AddProjectAuthorization()
                .AddSystemAuthorization()
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

        /// <summary>
        /// Registers project-level authorization policies based on <see cref="ProjectRole"/> hierarchy.
        /// Adds handlers for role-based checks on project routes (Reader, Member, Admin, Owner).
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <returns>The same service collection for chaining.</returns>
        private static IServiceCollection AddProjectAuthorization(this IServiceCollection services)
        {
            services.AddScoped<IAuthorizationHandler, ProjectRoleAuthorizationHandler>()
                .AddAuthorizationBuilder()
                .AddPolicy(
                    Policies.ProjectReader,
                    p => p.AddRequirements(new ProjectRoleRequirement(ProjectRole.Reader)))
                .AddPolicy(
                    Policies.ProjectMember,
                    p => p.AddRequirements(new ProjectRoleRequirement(ProjectRole.Member)))
                .AddPolicy(
                    Policies.ProjectAdmin,
                    p => p.AddRequirements(new ProjectRoleRequirement(ProjectRole.Admin)))
                .AddPolicy(
                    Policies.ProjectOwner,
                    p => p.AddRequirements(new ProjectRoleRequirement(ProjectRole.Owner)));

            return services;
        }

        /// <summary>
        /// Registers system-level authorization policies based on <see cref="UserRole"/>.
        /// Adds handlers for global administrative access (SystemAdmin).
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <returns>The same service collection for chaining.</returns>
        private static IServiceCollection AddSystemAuthorization(this IServiceCollection services)
        {
            services.AddScoped<IAuthorizationHandler, UserRoleAuthorizationHandler>()
                .AddAuthorizationBuilder()
                .AddPolicy(
                    Policies.SystemAdmin,
                    u => u.AddRequirements(new UserRoleRequirement(UserRole.Admin)));

            return services;
        }
    }
}
