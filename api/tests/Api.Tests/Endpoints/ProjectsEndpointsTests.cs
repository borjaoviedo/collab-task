using Api.Tests.Testing;
using Application.Projects.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TestHelpers.Api;

namespace Api.Tests.Endpoints
{
    public sealed class ProjectsEndpointsTests
    {
        [Fact]
        public async Task Create_Then_List_Shows_Project_For_User()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var auth = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

            var create = await client.PostAsJsonAsync("/projects", new ProjectCreateDto() { Name = "My Project"});
            create.StatusCode.Should().Be(HttpStatusCode.Created);

            var list = await client.GetAsync("/projects");
            list.StatusCode.Should().Be(HttpStatusCode.OK);

            var items = await list.Content.ReadFromJsonAsync<List<ProjectReadDto>>(EndpointsTestHelper.Json);
            items.Should().NotBeNull();
            items!.Any().Should().BeTrue();
            items!.Should().Contain(p => p != null && p.Name == "My Project");
        }

        [Fact]
        public async Task Get_ById_Returns200_For_Owner()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var auth = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

            var create = await client.PostAsJsonAsync("/projects", new ProjectCreateDto() { Name = "P1" });
            create.StatusCode.Should().Be(HttpStatusCode.Created);

            var list = await client.GetFromJsonAsync<List<ProjectReadDto>>("/projects", EndpointsTestHelper.Json);
            var prj = list!.Single(p => p.Name == "P1");

            var resp = await client.GetAsync($"/projects/{prj.Id}");
            resp.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Rename_Returns200_For_Admin_Or_Owner()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var auth = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", auth.AccessToken);

            (await client.PostAsJsonAsync("/projects", new ProjectCreateDto() { Name = "ToRename" }))
                .EnsureSuccessStatusCode();

            var prj = (await client.GetFromJsonAsync<List<ProjectReadDto>>("/projects", EndpointsTestHelper.Json))!
                .Single(p => p.Name == "ToRename");

            // If-Match header from current RowVersion
            EndpointsTestHelper.SetIfMatchFromRowVersion(client, prj.RowVersion);

            // Align DTO and route
            var resp = await client.PatchAsJsonAsync($"/projects/{prj.Id}/rename", new ProjectRenameDto() { NewName =  "Renamed"});

            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await resp.Content.ReadFromJsonAsync<ProjectReadDto>();
            body!.Name.Should().Be("Renamed");
        }

        [Fact]
        public async Task Delete_Returns204_For_Owner()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var auth = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", auth.AccessToken);

            (await client.PostAsJsonAsync("/projects", new ProjectCreateDto() { Name = "ToDelete" }))
                .EnsureSuccessStatusCode();

            var prj = (await client.GetFromJsonAsync<List<ProjectReadDto>>("/projects", EndpointsTestHelper.Json))!
                .Single(p => p.Name == "ToDelete");

            // If-Match header from current RowVersion
            var base64 = Convert.ToBase64String(prj.RowVersion);
            var req = new HttpRequestMessage(HttpMethod.Delete, $"/projects/{prj.Id}");
            req.Headers.TryAddWithoutValidation("If-Match", $"W/\"{base64}\"");

            // No body for DELETE
            var resp = await client.SendAsync(req);

            resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Create_Then_List_And_GetById_Sends_ETag()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var owner = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);

            // Create
            client.DefaultRequestHeaders.IfMatch.Clear();
            var create = await client.PostAsJsonAsync("/projects", new ProjectCreateDto { Name = "My Project" });
            create.StatusCode.Should().Be(HttpStatusCode.Created);

            // List
            var list = await client.GetAsync("/projects");
            list.StatusCode.Should().Be(HttpStatusCode.OK);
            var items = await list.Content.ReadFromJsonAsync<ProjectReadDto[]>(EndpointsTestHelper.Json);
            items!.Should().ContainSingle(p => p.Name == "My Project");

            // GetById -> ETag
            var prj = items.Single(p => p.Name == "My Project");
            var get = await client.GetAsync($"/projects/{prj.Id}");
            get.StatusCode.Should().Be(HttpStatusCode.OK);
            get.Headers.ETag.Should().NotBeNull();
        }

        [Fact]
        public async Task Get_ById_403_For_Foreign_User()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var a = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", a.AccessToken);
            (await client.PostAsJsonAsync("/projects", new ProjectCreateDto { Name = "P1" })).EnsureSuccessStatusCode();
            var prj = (await client.GetFromJsonAsync<ProjectReadDto[]>("/projects", EndpointsTestHelper.Json))!.Single(p => p.Name == "P1");

            var b = await EndpointsTestHelper.RegisterAndLoginAsync(client, name: "other", email: "other@x.com");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", b.AccessToken);

            var resp = await client.GetAsync($"/projects/{prj.Id}");
            resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Rename_Valid_Then_Stale_412_And_Missing_428()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var owner = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);

            (await client.PostAsJsonAsync("/projects", new ProjectCreateDto { Name = "ToRename" })).EnsureSuccessStatusCode();
            var prj = (await client.GetFromJsonAsync<ProjectReadDto[]>("/projects", EndpointsTestHelper.Json))!.Single(p => p.Name == "ToRename");

            // OK with valid If-Match
            EndpointsTestHelper.SetIfMatchFromRowVersion(client, prj.RowVersion);
            var ok = await client.PatchAsJsonAsync($"/projects/{prj.Id}/rename", new ProjectRenameDto { NewName = "Renamed" });
            ok.StatusCode.Should().Be(HttpStatusCode.OK);
            var renamed = await ok.Content.ReadFromJsonAsync<ProjectReadDto>(EndpointsTestHelper.Json);

            // Stale 412 (use old rowversion)
            EndpointsTestHelper.SetIfMatchFromRowVersion(client, prj.RowVersion);
            var stale = await client.PatchAsJsonAsync($"/projects/{renamed!.Id}/rename", new ProjectRenameDto { NewName = "Again" });
            stale.StatusCode.Should().Be((HttpStatusCode)412);

            // Missing If-Match -> 428
            client.DefaultRequestHeaders.IfMatch.Clear();
            var missing = await client.PatchAsJsonAsync($"/projects/{renamed.Id}/rename", new ProjectRenameDto { NewName = "PX" });
            missing.StatusCode.Should().Be((HttpStatusCode)428);
        }

        [Fact]
        public async Task Delete_204_With_IfMatch_And_428_When_Missing()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var owner = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);

            (await client.PostAsJsonAsync("/projects", new ProjectCreateDto { Name = "ToDelete" })).EnsureSuccessStatusCode();
            var prj = (await client.GetFromJsonAsync<ProjectReadDto[]>("/projects", EndpointsTestHelper.Json))!.Single(p => p.Name == "ToDelete");

            // 428 missing If-Match
            client.DefaultRequestHeaders.IfMatch.Clear();
            var miss = await client.DeleteAsync($"/projects/{prj.Id}");
            miss.StatusCode.Should().Be((HttpStatusCode)428);

            // 204 with If-Match
            EndpointsTestHelper.SetIfMatchFromRowVersion(client, prj.RowVersion);
            var del = await client.DeleteAsync($"/projects/{prj.Id}");
            del.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Me_200_And_ByUser_403()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var user = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);

            // create two projects
            await client.PostAsJsonAsync("/projects", new ProjectCreateDto { Name = "PA" });
            await client.PostAsJsonAsync("/projects", new ProjectCreateDto { Name = "PB" });

            var me = await client.GetAsync("/projects/me");
            me.StatusCode.Should().Be(HttpStatusCode.OK);

            // /users/{id} requires SystemAdmin
            var byUserForbidden = await client.GetAsync($"/projects/users/{user.UserId}");
            byUserForbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}
