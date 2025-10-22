using Application.Common.Abstractions.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Api.Auth.Services
{
    public sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
    {
        private readonly IHttpContextAccessor _accessor = accessor;
        private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

        public Guid? UserId
        {
            get
            {
                var id = Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

                return Guid.TryParse(id, out var guid) ? guid : null;
            }
        }
    }
}
