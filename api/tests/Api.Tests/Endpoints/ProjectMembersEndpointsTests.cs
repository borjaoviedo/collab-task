using Api.Tests.Testing;
using Application.ProjectMembers.DTOs;
using Application.Projects.DTOs;
using Application.Users.DTOs;
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
            var owner = await RegisterAndLogin(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);
            (await client.PostAsJsonAsync("/projects", new ProjectCreateDto() { Name = "Team"})).EnsureSuccessStatusCode();
            var prj = (await client.GetFromJsonAsync<List<ProjectReadDto>>("/projects", AuthTestHelper.Json))!
                .Single(p => p.Name == "Team");

            // another user to be added
            var member = await RegisterAndLogin(client, "Non Default User Name");

            // add as Member (Requires ProjectAdmin -> owner qualifies)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);
            var add = await client.PostAsJsonAsync(
                $"/projects/{prj.Id}/members",
                new ProjectMemberCreateDto() { UserId = member.UserId, Role = ProjectRole.Member, JoinedAt = DateTimeOffset.UtcNow });
            add.StatusCode.Should().Be(HttpStatusCode.Created);

            // list members
            var list = await client.GetAsync($"/projects/{prj.Id}/members?includeRemoved=false");
            list.StatusCode.Should().Be(HttpStatusCode.OK);
            var items = await list.Content.ReadFromJsonAsync<List<ProjectMemberReadDto>>(AuthTestHelper.Json);
            var m = items!.Single(x => x.UserId == member.UserId);

            // change role to Admin (Requires ProjectOwner) -> send If-Match
            var etag1 = $"W/\"{Convert.ToBase64String(m.RowVersion)}\"";
            client.DefaultRequestHeaders.IfMatch.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", etag1);

            var change = await client.PatchAsJsonAsync(
                $"/projects/{prj.Id}/members/{member.UserId}/role",
                new ProjectMemberChangeRoleDto() { NewRole = ProjectRole.Admin});
            change.StatusCode.Should().Be(HttpStatusCode.OK);
            var changed = await change.Content.ReadFromJsonAsync<ProjectMemberReadDto>(AuthTestHelper.Json);
            changed!.Role.Should().Be(ProjectRole.Admin);

            // refetch to get new RowVersion
            items = await client.GetFromJsonAsync<List<ProjectMemberReadDto>>(
                $"/projects/{prj.Id}/members?includeRemoved=false", AuthTestHelper.Json);
            m = items!.Single(x => x.UserId == member.UserId);

            // remove (Requires ProjectAdmin) -> If-Match
            var etag2 = $"W/\"{Convert.ToBase64String(m.RowVersion)}\"";
            client.DefaultRequestHeaders.IfMatch.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", etag2);

            var rem = await client.PatchAsJsonAsync(
                $"/projects/{prj.Id}/members/{member.UserId}/remove",
                new ProjectMemberRemoveDto() { RemovedAt = DateTimeOffset.UtcNow });
            rem.StatusCode.Should().Be(HttpStatusCode.OK);
            var removedDto = await rem.Content.ReadFromJsonAsync<ProjectMemberReadDto>(AuthTestHelper.Json);
            removedDto!.RemovedAt.Should().NotBeNull();

            var listActive = await client.GetFromJsonAsync<List<ProjectMemberReadDto>>(
                $"/projects/{prj.Id}/members?includeRemoved=false", AuthTestHelper.Json);
            listActive!.Any(x => x.UserId == member.UserId).Should().BeFalse();

            var listRemoved = await client.GetFromJsonAsync<List<ProjectMemberReadDto>>(
                $"/projects/{prj.Id}/members?includeRemoved=true", AuthTestHelper.Json);
            listRemoved!.Any(x => x.UserId == member.UserId).Should().BeTrue();

            // restore (Requires ProjectAdmin) -> If-Match
            var removed = listRemoved!.Single(x => x.UserId == member.UserId);
            var etag3 = $"W/\"{Convert.ToBase64String(removed.RowVersion)}\"";
            client.DefaultRequestHeaders.IfMatch.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", etag3);

            var restore = await client.PatchAsJsonAsync(
                $"/projects/{prj.Id}/members/{member.UserId}/restore",
                new object()); // no body per endpoint
            restore.StatusCode.Should().Be(HttpStatusCode.OK);
            var restoredDto = await restore.Content.ReadFromJsonAsync<ProjectMemberReadDto>(AuthTestHelper.Json);
            restoredDto!.RemovedAt.Should().BeNull();
        }

        private static async Task<AuthTestHelper.AuthToken> RegisterAndLogin(HttpClient client, string userName = "Test User")
        {
            var email = $"{Guid.NewGuid():N}@demo.com";
            var name = userName;
            var password = "Str0ngP@ss!";
            (await client.PostAsJsonAsync("/auth/register", new UserRegisterDto() { Email = email, Name = name, Password = password})).EnsureSuccessStatusCode();
            var login = await client.PostAsJsonAsync("/auth/login", new { email, password });
            login.EnsureSuccessStatusCode();
            var dto = await login.Content.ReadFromJsonAsync<AuthTestHelper.AuthToken>(AuthTestHelper.Json);
            return dto!;
        }
    }
}
