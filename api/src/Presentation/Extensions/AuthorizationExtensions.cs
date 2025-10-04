using Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

            services.AddAuthorization();
            return services;
        }
    }
}
