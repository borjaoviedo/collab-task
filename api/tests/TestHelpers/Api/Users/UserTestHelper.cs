using Application.Users.DTOs;
using TestHelpers.Api.Defaults;
using TestHelpers.Api.Http;

namespace TestHelpers.Api.Users
{
    public static class UserTestHelper
    {

        // ----- GET USER BY ID -----

        public static async Task<HttpResponseMessage> GetUserByIdResponseAsync(HttpClient client, Guid userId)
        {
            var response = await client.GetAsync($"/users/{userId}");
            return response;
        }

        public static async Task<UserReadDto> GetUserByIdDtoAsync(HttpClient client, Guid userId)
        {
            var response = await GetUserByIdResponseAsync(client, userId);
            var user = await response.ReadContentAsDtoAsync<UserReadDto>();

            return user;
        }

        // ----- GET USER BY EMAIL -----

        public static async Task<HttpResponseMessage> GetUserByEmailResponseAsync(
            HttpClient client,
            string? email = null)
        {
            email ??= UserDefaults.DefaultEmail;

            var response = await client.GetAsync($"/users/by-email?email={Uri.EscapeDataString(email)}");
            return response;
        }

        public static async Task<UserReadDto> GetUserByEmailDtoAsync(
            HttpClient client,
            string? email = null)
        {
            var response = await GetUserByEmailResponseAsync(client, email);
            var user = await response.ReadContentAsDtoAsync<UserReadDto>();

            return user;
        }

        // ----- GET USERS -----

        public static async Task<HttpResponseMessage> GetUsersResponseAsync(HttpClient client)
        {
            var response = await client.GetAsync($"/users");
            return response;
        }

        // ----- PUT RENAME -----

        public static async Task<HttpResponseMessage> RenameUserResponseAsync(
            HttpClient client,
            Guid userId,
            byte[] rowVersion,
            UserRenameDto? dto = null)
        {
            var newName = dto is null ? UserDefaults.DefaultUserRename : dto.NewName;
            var renameDto = new UserRenameDto() { NewName = newName };

            var renameResponse = await client.PatchWithIfMatchAsync(
                rowVersion,
                $"/users/{userId}/rename",
                renameDto);

            return renameResponse;
        }

        public static async Task<UserReadDto> RenameUserDtoAsync(
            HttpClient client,
            Guid userId,
            byte[] rowVersion,
            UserRenameDto? dto = null)
        {
            var renameResponse = await RenameUserResponseAsync(client, userId, rowVersion, dto);
            var user = await renameResponse.ReadContentAsDtoAsync<UserReadDto>();

            return user;
        }

        // ----- PATCH ROLE -----

        public static async Task<HttpResponseMessage> ChangeRoleResponseAsync(
            HttpClient client,
            Guid userId,
            byte[] rowVersion,
            UserChangeRoleDto? dto = null)
        {
            var newRole = dto is null ? UserDefaults.DefaultUserChangeRole : dto.NewRole;
            var changeRoleDto = new UserChangeRoleDto() { NewRole = newRole };

            var renameResponse = await client.PatchWithIfMatchAsync(
                rowVersion,
                $"/users/{userId}/role",
                changeRoleDto);

            return renameResponse;
        }

        public static async Task<UserReadDto> ChangeRoleUserDtoAsync(
            HttpClient client,
            Guid userId,
            byte[] rowVersion,
            UserChangeRoleDto? dto = null)
        {
            var renameResponse = await ChangeRoleResponseAsync(client, userId, rowVersion, dto);
            var user = await renameResponse.ReadContentAsDtoAsync<UserReadDto>();

            return user;
        }

        // ----- DELETE -----

        public static async Task<HttpResponseMessage> DeleteUserResponseAsync(
            HttpClient client,
            Guid userId,
            byte[] rowVersion)
        {
            var deleteResponse = await client.DeleteWithIfMatchAsync(
                rowVersion,
                $"/users/{userId}");

            return deleteResponse;
        }
    }
}
