using Api.Tests.Testing;
using Application.ProjectMembers.DTOs;
using Application.Projects.DTOs;
using Domain.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TestHelpers;

namespace Api.Tests.Endpoints
{
    public sealed class ProjectMembersEndpointsTests
    {
        [Fact]
        public async Task Admin_Adds_ChangesRole_Removes_And_Restores_Member()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            // owner creates project
            var owner = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);
            (await client.PostAsJsonAsync("/projects", new ProjectCreateDto() { Name = "Team"})).EnsureSuccessStatusCode();
            var prj = (await client.GetFromJsonAsync<List<ProjectReadDto>>("/projects", EndpointsTestHelper.Json))!
                .Single(p => p.Name == "Team");

            // another user to be added
            var member = await EndpointsTestHelper.RegisterAndLoginAsync(client, name: "Non Default User Name");

            // add as Member (Requires ProjectAdmin -> owner qualifies)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);
            var add = await client.PostAsJsonAsync(
                $"/projects/{prj.Id}/members",
                new ProjectMemberCreateDto() { UserId = member.UserId, Role = ProjectRole.Member });
            add.StatusCode.Should().Be(HttpStatusCode.Created);

            // list members
            var list = await client.GetAsync($"/projects/{prj.Id}/members?includeRemoved=false");
            list.StatusCode.Should().Be(HttpStatusCode.OK);
            var items = await list.Content.ReadFromJsonAsync<List<ProjectMemberReadDto>>(EndpointsTestHelper.Json);
            var m = items!.Single(x => x.UserId == member.UserId);

            // change role to Admin (Requires ProjectOwner) -> send If-Match
            EndpointsTestHelper.SetIfMatchFromRowVersion(client, m.RowVersion);
            var change = await client.PatchAsJsonAsync(
                $"/projects/{prj.Id}/members/{member.UserId}/role",
                new ProjectMemberChangeRoleDto() { NewRole = ProjectRole.Admin});
            change.StatusCode.Should().Be(HttpStatusCode.OK);
            var changed = await change.Content.ReadFromJsonAsync<ProjectMemberReadDto>(EndpointsTestHelper.Json);
            changed!.Role.Should().Be(ProjectRole.Admin);

            // refetch to get new RowVersion
            items = await client.GetFromJsonAsync<List<ProjectMemberReadDto>>(
                $"/projects/{prj.Id}/members?includeRemoved=false", EndpointsTestHelper.Json);
            m = items!.Single(x => x.UserId == member.UserId);

            // remove (Requires ProjectAdmin) -> If-Match
            EndpointsTestHelper.SetIfMatchFromRowVersion(client, m.RowVersion);
            var rem = await client.PatchAsJsonAsync(
                $"/projects/{prj.Id}/members/{member.UserId}/remove", new object());
            rem.StatusCode.Should().Be(HttpStatusCode.OK);
            var removedDto = await rem.Content.ReadFromJsonAsync<ProjectMemberReadDto>(EndpointsTestHelper.Json);
            removedDto!.RemovedAt.Should().NotBeNull();

            var listActive = await client.GetFromJsonAsync<List<ProjectMemberReadDto>>(
                $"/projects/{prj.Id}/members?includeRemoved=false", EndpointsTestHelper.Json);
            listActive!.Any(x => x.UserId == member.UserId).Should().BeFalse();

            var listRemoved = await client.GetFromJsonAsync<List<ProjectMemberReadDto>>(
                $"/projects/{prj.Id}/members?includeRemoved=true", EndpointsTestHelper.Json);
            listRemoved!.Any(x => x.UserId == member.UserId).Should().BeTrue();

            // restore (Requires ProjectAdmin) -> If-Match
            var removed = listRemoved!.Single(x => x.UserId == member.UserId);
            EndpointsTestHelper.SetIfMatchFromRowVersion(client, removed.RowVersion);

            var restore = await client.PatchAsJsonAsync(
                $"/projects/{prj.Id}/members/{member.UserId}/restore",
                new object()); // no body per endpoint
            restore.StatusCode.Should().Be(HttpStatusCode.OK);
            var restoredDto = await restore.Content.ReadFromJsonAsync<ProjectMemberReadDto>(EndpointsTestHelper.Json);
            restoredDto!.RemovedAt.Should().BeNull();
        }

        [Fact]
        public async Task List_Empty_Create_GetById_Sends_ETag()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var owner = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);

            // owner project
            var prj = await EndpointsTestHelper.CreateProject(client);

            // empty list
            var list0 = await client.GetAsync($"/projects/{prj.Id}/members?includeRemoved=false");
            list0.StatusCode.Should().Be(HttpStatusCode.OK);
            var items0 = await list0.Content.ReadFromJsonAsync<ProjectMemberReadDto[]>(EndpointsTestHelper.Json);
            items0!.Should().ContainSingle(m => m.UserId == owner.UserId && m.Role == ProjectRole.Owner);

            // create a second user
            var user = await EndpointsTestHelper.RegisterAndLoginAsync(client, name: "Other user name");

            // add member (POST rejects If-Match, so ensure cleared)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);
            client.DefaultRequestHeaders.IfMatch.Clear();
            var add = await client.PostAsJsonAsync(
                $"/projects/{prj.Id}/members",
                new ProjectMemberCreateDto { UserId = user.UserId, Role = ProjectRole.Member });
            add.StatusCode.Should().Be(HttpStatusCode.Created);

            // get by id -> ETag present
            var get = await client.GetAsync($"/projects/{prj.Id}/members/{user.UserId}");
            get.StatusCode.Should().Be(HttpStatusCode.OK);
            get.Headers.ETag.Should().NotBeNull();
        }

        [Fact]
        public async Task Create_With_IfMatch_Header_Returns_400()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var owner = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);

            var prj = await EndpointsTestHelper.CreateProject(client);
            var user = await EndpointsTestHelper.RegisterAndLoginAsync(client, name: "Other user name");

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);
            client.DefaultRequestHeaders.IfMatch.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", "\"abc\"");

            var add = await client.PostAsJsonAsync(
                $"/projects/{prj.Id}/members",
                new ProjectMemberCreateDto { UserId = user.UserId, Role = ProjectRole.Member });
            add.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ChangeRole_Valid_Then_Stale_Then_Missing_IfMatch()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var owner = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);

            var prj = await EndpointsTestHelper.CreateProject(client);
            var user = await EndpointsTestHelper.RegisterAndLoginAsync(client, name: "Other user name");

            // add
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);
            client.DefaultRequestHeaders.IfMatch.Clear();
            (await client.PostAsJsonAsync(
                $"/projects/{prj.Id}/members",
                new ProjectMemberCreateDto { UserId = user.UserId, Role = ProjectRole.Member }))
                .EnsureSuccessStatusCode();

            // list to get rowversion
            var list = await client.GetFromJsonAsync<ProjectMemberReadDto[]>($"/projects/{prj.Id}/members?includeRemoved=false", EndpointsTestHelper.Json);
            var m = list!.Single(x => x.UserId == user.UserId);

            // valid change role
            EndpointsTestHelper.SetIfMatchFromRowVersion(client, m.RowVersion);
            var ok = await client.PatchAsJsonAsync(
                $"/projects/{prj.Id}/members/{user.UserId}/role",
                new ProjectMemberChangeRoleDto { NewRole = ProjectRole.Admin });
            ok.StatusCode.Should().Be(HttpStatusCode.OK);

            // stale change role
            EndpointsTestHelper.SetIfMatchFromRowVersion(client, m.RowVersion); // old
            var stale = await client.PatchAsJsonAsync(
                $"/projects/{prj.Id}/members/{user.UserId}/role",
                new ProjectMemberChangeRoleDto { NewRole = ProjectRole.Member });
            stale.StatusCode.Should().Be((HttpStatusCode)412);

            // missing If-Match -> 428
            client.DefaultRequestHeaders.IfMatch.Clear();
            var miss = await client.PatchAsJsonAsync(
                $"/projects/{prj.Id}/members/{user.UserId}/role",
                new ProjectMemberChangeRoleDto { NewRole = ProjectRole.Member });
            miss.StatusCode.Should().Be((HttpStatusCode)428);
        }

        [Fact]
        public async Task Remove_Then_Restore_With_Concurrency()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var owner = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);

            var prj = await EndpointsTestHelper.CreateProject(client);
            var user = await EndpointsTestHelper.RegisterAndLoginAsync(client, name: "Other user name");

            // add
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);
            client.DefaultRequestHeaders.IfMatch.Clear();
            (await client.PostAsJsonAsync(
                $"/projects/{prj.Id}/members",
                new ProjectMemberCreateDto { UserId = user.UserId, Role = ProjectRole.Member }))
                .EnsureSuccessStatusCode();

            // fetch rowversion
            var list = await client.GetFromJsonAsync<ProjectMemberReadDto[]>($"/projects/{prj.Id}/members?includeRemoved=false", EndpointsTestHelper.Json);
            var m = list!.Single(x => x.UserId == user.UserId);

            // remove
            EndpointsTestHelper.SetIfMatchFromRowVersion(client, m.RowVersion);
            var rem = await client.PatchAsJsonAsync($"/projects/{prj.Id}/members/{user.UserId}/remove", new { });
            rem.StatusCode.Should().Be(HttpStatusCode.OK);

            // verify not in active, yes in removed
            var active = await client.GetFromJsonAsync<ProjectMemberReadDto[]>($"/projects/{prj.Id}/members?includeRemoved=false", EndpointsTestHelper.Json);
            active!.Any(x => x.UserId == user.UserId).Should().BeFalse();
            var removed = await client.GetFromJsonAsync<ProjectMemberReadDto[]>($"/projects/{prj.Id}/members?includeRemoved=true", EndpointsTestHelper.Json);
            removed!.Any(x => x.UserId == user.UserId).Should().BeTrue();

            // restore with current rowversion
            var removedEntry = removed!.Single(x => x.UserId == user.UserId);
            EndpointsTestHelper.SetIfMatchFromRowVersion(client, removedEntry.RowVersion);
            var res = await client.PatchAsJsonAsync($"/projects/{prj.Id}/members/{user.UserId}/restore", new { });
            res.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetById_404_Vs_403_And_GetRole_404()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            // user A
            var a = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", a.AccessToken);

            var prj = await EndpointsTestHelper.CreateProject(client);

            // 404: member not in project
            var notFound = await client.GetAsync($"/projects/{prj.Id}/members/{Guid.NewGuid()}");
            notFound.StatusCode.Should().Be(HttpStatusCode.NotFound);

            // user B tries to access A's project -> 403
            var b = await EndpointsTestHelper.RegisterAndLoginAsync(client, name: "buser", email: "b@x.com");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", b.AccessToken);
            var forbidden = await client.GetAsync($"/projects/{prj.Id}/members/{Guid.NewGuid()}");
            forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            // Get role 404 (non-member)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", a.AccessToken);
            var role404 = await client.GetAsync($"/projects/{prj.Id}/members/{Guid.NewGuid()}/role");
            role404.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Count_Me_200_Unauthorized_401_And_Admin_ByUser_403()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            // unauthorized -> 401
            var unauth = await client.GetAsync("/members/me/count");
            unauth.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            // login normal user
            var user = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);

            // create two projects to own
            await EndpointsTestHelper.CreateProject(client);
            await EndpointsTestHelper.CreateProject(client);

            var me = await client.GetAsync("/members/me/count");
            me.StatusCode.Should().Be(HttpStatusCode.OK);
            var meCount = await me.Content.ReadFromJsonAsync<ProjectMemberCountReadDto>(EndpointsTestHelper.Json);
            meCount!.Count.Should().BeGreaterThanOrEqualTo(1);

            // admin endpoint without admin -> 403
            var byUserForbidden = await client.GetAsync($"/members/{user.UserId}/count");
            byUserForbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}
