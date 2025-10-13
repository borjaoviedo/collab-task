using Api.Tests.Testing;
using Application.Projects.DTOs;
using Application.Users.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Api.Tests.Endpoints
{
    public sealed class ProjectsEndpointsTests
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
        private sealed record AuthToken(string AccessToken, Guid UserId, string Email, string Name, string Role);

        [Fact]
        public async Task Create_Then_List_Shows_Project_For_User()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var auth = await RegisterAndLogin(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

            var create = await client.PostAsJsonAsync("/projects", new ProjectCreateDto() { Name = "My Project"});
            create.StatusCode.Should().Be(HttpStatusCode.Created);

            var list = await client.GetAsync("/projects");
            list.StatusCode.Should().Be(HttpStatusCode.OK);

            var items = await list.Content.ReadFromJsonAsync<List<ProjectReadDto>>(Json);
            items.Should().NotBeNull();
            items!.Any().Should().BeTrue();
            items!.Should().Contain(p => p != null && p.Name == "My Project");
        }

        [Fact]
        public async Task Get_ById_Returns200_For_Owner()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var auth = await RegisterAndLogin(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

            var create = await client.PostAsJsonAsync("/projects", new ProjectCreateDto() { Name = "P1" });
            create.StatusCode.Should().Be(HttpStatusCode.Created);

            var list = await client.GetFromJsonAsync<List<ProjectReadDto>>("/projects", Json);
            var prj = list!.Single(p => p.Name == "P1");

            var resp = await client.GetAsync($"/projects/{prj.Id}");
            resp.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Rename_Returns200_For_Admin_Or_Owner()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var auth = await RegisterAndLogin(client);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", auth.AccessToken);

            (await client.PostAsJsonAsync("/projects", new ProjectCreateDto() { Name = "ToRename" }))
                .EnsureSuccessStatusCode();

            var prj = (await client.GetFromJsonAsync<List<ProjectReadDto>>("/projects", Json))!
                .Single(p => p.Name == "ToRename");

            // If-Match header from current RowVersion
            var base64 = Convert.ToBase64String(prj.RowVersion);
            client.DefaultRequestHeaders.IfMatch.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", $"W/\"{base64}\"");

            // Align DTO and route
            var resp = await client.PatchAsJsonAsync($"/projects/{prj.Id}/rename",
                new ProjectRenameDto() { NewName =  "Renamed"});

            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await resp.Content.ReadFromJsonAsync<ProjectReadDto>();
            body!.Name.Should().Be("Renamed");
        }

        [Fact]
        public async Task Delete_Returns204_For_Owner()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var auth = await RegisterAndLogin(client);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", auth.AccessToken);

            (await client.PostAsJsonAsync("/projects", new ProjectCreateDto() { Name = "ToDelete" }))
                .EnsureSuccessStatusCode();

            var prj = (await client.GetFromJsonAsync<List<ProjectReadDto>>("/projects", Json))!
                .Single(p => p.Name == "ToDelete");

            // If-Match header from current RowVersion
            var base64 = Convert.ToBase64String(prj.RowVersion);
            var req = new HttpRequestMessage(HttpMethod.Delete, $"/projects/{prj.Id}");
            req.Headers.TryAddWithoutValidation("If-Match", $"W/\"{base64}\"");

            // No body for DELETE
            var resp = await client.SendAsync(req);

            resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        private static async Task<AuthToken> RegisterAndLogin(HttpClient client)
        {
            var email = $"{Guid.NewGuid():N}@demo.com";
            var name = "Test User";
            var password = "Str0ngP@ss!";

            (await client.PostAsJsonAsync("/auth/register", new UserRegisterDto() { Email = email, Name = name, Password = password})).EnsureSuccessStatusCode();

            var login = await client.PostAsJsonAsync("/auth/login", new { email, password });
            login.EnsureSuccessStatusCode();

            var dto = await login.Content.ReadFromJsonAsync<AuthToken>(Json);
            return dto!;
        }
    }
}
