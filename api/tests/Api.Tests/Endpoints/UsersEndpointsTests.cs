using Api.Tests.Testing;
using Application.Abstractions.Security;
using Application.Users.DTOs;
using Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using TestHelpers.Api.Auth;
using TestHelpers.Api.Defaults;
using TestHelpers.Api.Http;
using TestHelpers.Api.Users;

namespace Api.Tests.Endpoints
{
    public sealed class UsersEndpointsTests
    {
        [Fact]
        public async Task Get_ById_Returns200_When_Admin()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            // create subject user
            var user = await AuthTestHelper.PostRegisterAndLoginAsync(client);

            // mint admin token
            var adminBearer = await MintTokenAsync(app, user.UserId, user.Email, user.Name, UserRole.Admin);
            client.SetAuthorization(adminBearer);

            var dto = await UserTestHelper.GetUserByIdDtoAsync(client, user.UserId);
            dto!.Id.Should().Be(user.UserId);
            dto.Email.Should().Be(user.Email);
        }

        [Fact]
        public async Task Get_ById_Returns403_When_Not_Admin()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var user = await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);

            var response = await UserTestHelper.GetUserByIdResponseAsync(client, user.UserId);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Get_All_Returns200_When_Admin()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var user = await AuthTestHelper.PostRegisterAndLoginAsync(client);
            var adminBearer = await MintTokenAsync(app, user.UserId, user.Email, user.Name, UserRole.Admin);
            client.SetAuthorization(adminBearer);

            var response = await UserTestHelper.GetUsersResponseAsync(client);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Get_All_Returns403_When_Not_Admin()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);

            var response = await UserTestHelper.GetUsersResponseAsync(client);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Change_Role_Returns200_When_Admin()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var target = await AuthTestHelper.PostRegisterAndLoginAsync(client);
            var adminBearer = await MintTokenAsync(app, target.UserId, target.Email, target.Name, UserRole.Admin);
            client.SetAuthorization(adminBearer);

            var currentUser = await UserTestHelper.GetUserByIdDtoAsync(client, target.UserId);
            var dto = await UserTestHelper.ChangeRoleUserDtoAsync(
                client,
                currentUser.Id,
                currentUser.RowVersion);

            dto.Role.Should().Be(UserRole.Admin);
        }

        [Fact]
        public async Task Delete_Returns204_When_Admin()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var victim = await AuthTestHelper.PostRegisterAndLoginAsync(client);
            var adminBearer = await MintTokenAsync(app, victim.UserId, victim.Email, victim.Name, UserRole.Admin);
            client.SetAuthorization(adminBearer);

            var currentUser = await UserTestHelper.GetUserByIdDtoAsync(client, victim.UserId);
            var etag = $"W/\"{Convert.ToBase64String(currentUser.RowVersion)}\"";
            var request = new HttpRequestMessage(HttpMethod.Delete, $"/users/{victim.UserId}");

            IfMatchExtensions.SetIfMatchFromETag(request, etag);
            var response = await client.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Get_ById_And_List_Admin_vs_NotAdmin()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var user = await AuthTestHelper.PostRegisterAndLoginAsync(client);
            var adminBearer = await MintTokenAsync(app, user.UserId, user.Email, user.Name, UserRole.Admin);

            // /users (admin OK, non-admin 403)
            client.SetAuthorization(adminBearer);
            var getUsersResponse = await UserTestHelper.GetUsersResponseAsync(client);
            getUsersResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            client.SetAuthorization(user.AccessToken);
            getUsersResponse = await UserTestHelper.GetUsersResponseAsync(client);
            getUsersResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            // /users/{id} (admin OK, non-admin 403)
            client.SetAuthorization(adminBearer);
            var getUserResponse = await UserTestHelper.GetUserByIdResponseAsync(client, user.UserId);
            getUserResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            getUserResponse.Headers.ETag.Should().NotBeNull();

            client.SetAuthorization(user.AccessToken);
            getUserResponse = await UserTestHelper.GetUserByIdResponseAsync(client, user.UserId);
            getUserResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Rename_Self_Valid_200_Stale_412_Missing_428()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var user = await AuthTestHelper.PostRegisterAndLoginAsync(client);

            // Elevate to admin to read current user by id
            var adminBearer = await MintTokenAsync(app, user.UserId, user.Email, user.Name, UserRole.Admin);
            client.SetAuthorization(adminBearer);

            var currentUser = await UserTestHelper.GetUserByIdDtoAsync(client, user.UserId);

            // Back to self auth for rename flow
            client.SetAuthorization(user.AccessToken);

            // Self rename with valid If-Match
            client.DefaultRequestHeaders.IfMatch.Clear();
            var okRenameResponse = await UserTestHelper.RenameUserResponseAsync(
                client,
                currentUser.Id,
                currentUser.RowVersion);
            okRenameResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Stale 412 (reuse old rowversion)
            client.DefaultRequestHeaders.IfMatch.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "If-Match",
                $"W/\"{Convert.ToBase64String(currentUser.RowVersion)}\"");

            var secondRenameDto = new UserRenameDto { NewName = "second" };
            var staleRenameResponse = await UserTestHelper.RenameUserResponseAsync(
                client,
                currentUser.Id,
                currentUser.RowVersion,
                secondRenameDto);
            staleRenameResponse.StatusCode.Should().Be((HttpStatusCode)412);

            // Missing 428
            client.DefaultRequestHeaders.IfMatch.Clear();
            var thirdRenameDto = new UserRenameDto { NewName = "third" };
            var missingRenameResponse = await client.PatchAsJsonAsync(
                $"/users/{user.UserId}/rename",
                thirdRenameDto);
            missingRenameResponse.StatusCode.Should().Be((HttpStatusCode)428);
        }

        [Fact]
        public async Task ChangeRole_Admin_200_Stale_412_Missing_428_And_403_NotAdmin()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var user = await AuthTestHelper.PostRegisterAndLoginAsync(client);
            var adminBearer = await MintTokenAsync(app, user.UserId, user.Email, user.Name, UserRole.Admin);
            client.SetAuthorization(adminBearer);
            var currentUser = await UserTestHelper.GetUserByIdDtoAsync(client, user.UserId);

            // Admin OK
            client.DefaultRequestHeaders.IfMatch.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "If-Match",
                $"W/\"{Convert.ToBase64String(currentUser.RowVersion)}\"");

            var okChangeRoleResponse = await client.PatchAsJsonAsync(
                $"/users/{user.UserId}/role",
                UserDefaults.DefaultUserChangeRoleDto);
            okChangeRoleResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Stale 412
            client.DefaultRequestHeaders.IfMatch.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "If-Match",
                $"W/\"{Convert.ToBase64String(currentUser.RowVersion)}\"");

            var differentChangeRoleDto = new UserChangeRoleDto { NewRole = UserRole.User };
            var stale = await client.PatchAsJsonAsync($"/users/{user.UserId}/role", differentChangeRoleDto);
            stale.StatusCode.Should().Be((HttpStatusCode)412);

            var staleChangeRoleResponse = await UserTestHelper.ChangeRoleResponseAsync(
                client,
                currentUser.Id,
                currentUser.RowVersion,
                differentChangeRoleDto);
            staleChangeRoleResponse.StatusCode.Should().Be((HttpStatusCode)412);

            // Missing 428
            client.DefaultRequestHeaders.IfMatch.Clear();
            var missingChangeRoleResponse = await client.PatchAsJsonAsync($"/users/{user.UserId}/role", differentChangeRoleDto);
            missingChangeRoleResponse.StatusCode.Should().Be((HttpStatusCode)428);

            // 403 not admin
            client.SetAuthorization(user.AccessToken);
            client.DefaultRequestHeaders.IfMatch.Clear();
            var forbiddenChangeRoleResponse = await client.PatchAsJsonAsync($"/users/{user.UserId}/role", differentChangeRoleDto);
            forbiddenChangeRoleResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Delete_Admin_204_And_428_When_Missing()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var victim = await AuthTestHelper.PostRegisterAndLoginAsync(client);
            var adminBearer = await MintTokenAsync(app, victim.UserId, victim.Email, victim.Name, UserRole.Admin);
            client.SetAuthorization(adminBearer);

            var currentUser = await UserTestHelper.GetUserByIdDtoAsync(client, victim.UserId);

            // 428
            client.DefaultRequestHeaders.IfMatch.Clear();
            var deleteResponse = await client.DeleteAsync($"/users/{victim.UserId}");
            deleteResponse.StatusCode.Should().Be((HttpStatusCode)428);

            // 204
            var etag = $"W/\"{Convert.ToBase64String(currentUser.RowVersion)}\"";
            var request = new HttpRequestMessage(HttpMethod.Delete, $"/users/{victim.UserId}");

            request.Headers.TryAddWithoutValidation("If-Match", etag);
            deleteResponse = await client.SendAsync(request);
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        public static async Task<string> MintTokenAsync(TestApiFactory app, Guid userId, string email, string name, UserRole role)
        {
            await using var scope = app.Services.CreateAsyncScope();

            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var (token, _) = jwt.CreateToken(userId, email, name, role);

            return token;
        }
    }
}
