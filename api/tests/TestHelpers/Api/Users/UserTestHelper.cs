using Application.Users.DTOs;
using TestHelpers.Api.Http;

namespace TestHelpers.Api.Users
{
    public static class UserTestHelper
    {
        public static async Task<UserReadDto> GetUserAsync(HttpClient client, Guid userId, string adminBearer)
        {
            client.SetAuthorization(adminBearer);

            var response = await client.GetAsync($"/users/{userId}");
            response.EnsureSuccessStatusCode();

            var user = await response.ReadContentAsDtoAsync<UserReadDto>();
            return user;
        }
    }
}
