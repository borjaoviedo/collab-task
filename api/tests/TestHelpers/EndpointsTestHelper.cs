using Application.Lanes.DTOs;
using Application.Projects.DTOs;
using Application.Users.DTOs;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TestHelpers
{
    public static class EndpointsTestHelper
    {
        public sealed record AuthToken(string AccessToken, Guid UserId, string Email, string Name, string Role);

        public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };

        public static async Task<AuthToken> RegisterAndLoginAsync(HttpClient client, string? email = null, string name = "User Name", string password = "Str0ngP@ss!")
        {
            // ensure anonymous for register/login
            client.DefaultRequestHeaders.Authorization = null;

            email ??= $"{Guid.NewGuid():N}@demo.com";

            var register = await client.PostAsJsonAsync("/auth/register", new UserRegisterDto { Email = email, Name = name, Password = password });
            register.EnsureSuccessStatusCode();

            var login = await client.PostAsJsonAsync("/auth/login", new { Email = email, Password = password });
            login.EnsureSuccessStatusCode();

            var token = await login.Content.ReadFromJsonAsync<AuthToken>(Json);
            return token!;
        }

        public static async Task<ProjectReadDto> CreateProject(HttpClient client)
        => (await (await client.PostAsJsonAsync("/projects", new ProjectCreateDto() { Name = "Project 1"}))
            .Content.ReadFromJsonAsync<ProjectReadDto>(Json))!;

        public static async Task<LaneReadDto> CreateLane(HttpClient client, Guid projectId, string name = "L1", int order = 0)
        {
            client.DefaultRequestHeaders.IfMatch.Clear();
            return (await (await client.PostAsJsonAsync($"/projects/{projectId}/lanes", new LaneCreateDto() { Name = name, Order = order }))
                .Content.ReadFromJsonAsync<LaneReadDto>(Json))!;
        }
        public static async Task<HttpResponseMessage> PostWithoutIfMatchAsync<T>(HttpClient client, string url, T payload)
        {
            client.DefaultRequestHeaders.IfMatch.Clear();
            return await client.PostAsJsonAsync(url, payload);
        }

        public static void SetIfMatchFromRowVersion(HttpClient client, byte[] rowVersion)
        {
            client.DefaultRequestHeaders.IfMatch.Clear();
            var etag = $"W/\"{Convert.ToBase64String(rowVersion)}\"";
            client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", etag);
        }

        public static void SetIfMatchFromETag(HttpRequestMessage req, string etagTag)
        {
            // Accept both weak or strong from server, forward verbatim
            req.Headers.IfMatch.Clear();
            if (EntityTagHeaderValue.TryParse(etagTag, out var parsed))
                req.Headers.IfMatch.Add(parsed);
            else
                req.Headers.TryAddWithoutValidation("If-Match", etagTag);
        }

        public static async Task<(ProjectReadDto prj, LaneReadDto lane)> CreateProjectAndLane(HttpClient client, string laneName = "L1", int laneOrder = 0)
        {
            var prj = await CreateProject(client);
            var lane = await CreateLane(client, prj!.Id, laneName, laneOrder);
            return (prj, lane);
        }
    }
}
