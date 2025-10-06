using Api.Common;
using Application.Users.Abstractions;
using Domain.Enums;

namespace Api.Endpoints
{
    public static class UsersEndpoints
    {
        public sealed record RenameUserDto(string Name, byte[] RowVersion);
        public sealed record ChangeRoleDto(UserRole Role, byte[] RowVersion);
        public sealed record DeleteUserDto(byte[] RowVersion);

        public static IEndpointRouteBuilder MapUsers(this IEndpointRouteBuilder app)
        {
            var g = app.MapGroup("/users/{id:guid}").WithTags("Users");

            g.MapPatch("/name", async (Guid id, RenameUserDto dto, IUserService svc, CancellationToken ct) =>
            {
                var res = await svc.RenameAsync(id, dto.Name, dto.RowVersion, ct);
                return res.ToHttp();
            });

            g.MapPatch("/role", async (Guid id, ChangeRoleDto dto, IUserService svc, CancellationToken ct) =>
            {
                var res = await svc.ChangeRoleAsync(id, dto.Role, dto.RowVersion, ct);
                return res.ToHttp();
            });

            g.MapDelete("/", async (Guid id, DeleteUserDto dto, IUserService svc, CancellationToken ct) =>
            {
                var res = await svc.DeleteAsync(id, dto.RowVersion, ct);
                return res.ToHttp();
            });

            return app;
        }
    }
}
