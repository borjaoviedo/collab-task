using Api.Tests.Common.Helpers;
using Api.Tests.Testing;
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

        private sealed record RegisterReq(string Email, string Name, string Password);
        private sealed record AuthToken(string AccessToken, Guid UserId, string Email, string Name, string Role);

        private sealed record ProjectCreateDto(string Name);
        private sealed record ProjectReadDto(Guid Id, string Name, string Slug, string Role, byte[] RowVersion);
        private sealed record RenameProjectDto(string Name, byte[] RowVersion);
        private sealed record DeleteProjectDto(byte[] RowVersion);

        [Fact]
        public async Task Create_Then_List_Shows_Project_For_User()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var auth = await RegisterAndLogin(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

            var create = await client.PostAsJsonAsync("/projects", new ProjectCreateDto("My Project"));
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

            var create = await client.PostAsJsonAsync("/projects", new ProjectCreateDto("P1"));
            create.StatusCode.Should().Be(HttpStatusCode.Created);

            var list = await client.GetFromJsonAsync<List<ProjectReadDto>>("/projects", Json);
            var prj = list!.Single(p => p.Name == "P1");

            var resp = await client.GetAsync($"/projects/{prj.Id}");
            resp.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Rename_Returns204_For_Admin_Or_Owner()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var auth = await RegisterAndLogin(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

            (await client.PostAsJsonAsync("/projects", new ProjectCreateDto("ToRename"))).EnsureSuccessStatusCode();
            var prj = (await client.GetFromJsonAsync<List<ProjectReadDto>>("/projects", Json))!.Single(p => p.Name == "ToRename");

            var resp = await client.PatchAsJsonAsync($"/projects/{prj.Id}/name", new RenameProjectDto("Renamed", prj.RowVersion));
            resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Delete_Returns204_For_Owner()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var auth = await RegisterAndLogin(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

            (await client.PostAsJsonAsync("/projects", new ProjectCreateDto("ToDelete"))).EnsureSuccessStatusCode();
            var prj = (await client.GetFromJsonAsync<List<ProjectReadDto>>("/projects", Json))!.Single(p => p.Name == "ToDelete");

            var resp = await client.DeleteAsJsonAsync($"/projects/{prj.Id}", new DeleteProjectDto(prj.RowVersion));
            resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        

        private static async Task<AuthToken> RegisterAndLogin(HttpClient client)
        {
            var email = $"{Guid.NewGuid():N}@demo.com";
            var name = "Test User";
            var password = "Str0ngP@ss!";

            (await client.PostAsJsonAsync("/auth/register", new RegisterReq(email, name, password))).EnsureSuccessStatusCode();

            var login = await client.PostAsJsonAsync("/auth/login", new { email, password });
            login.EnsureSuccessStatusCode();

            var dto = await login.Content.ReadFromJsonAsync<AuthToken>(Json);
            return dto!;
        }
    }
}
