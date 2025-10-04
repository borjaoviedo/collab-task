using Application.Common.Abstractions.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Api.Auth
{
    public sealed class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _accessor;

        public CurrentUserService(IHttpContextAccessor accessor) => _accessor = accessor;

        private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

        public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

        public Guid? UserId
        {
            get
            {
                var id = Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                return Guid.TryParse(id, out var guid) ? guid : null;
            }
        }

        public string? Email => Principal?.FindFirst(ClaimTypes.Email)?.Value
            ?? Principal?.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
        public string? Name => Principal?.FindFirst(ClaimTypes.Name)?.Value;

        public string? Role => Principal?.FindFirst(ClaimTypes.Role)?.Value;
    }
}
