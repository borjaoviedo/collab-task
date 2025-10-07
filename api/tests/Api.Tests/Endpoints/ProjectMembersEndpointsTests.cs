using Api.Tests.Testing;
using Domain.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Api.Tests.Endpoints
{
    public sealed class ProjectMembersEndpointsTests
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        private sealed record RegisterReq(string Email, string Name, string Password);
        private sealed record AuthToken(string AccessToken, Guid UserId, string Email, string Name, string Role);

        private sealed record ProjectCreateDto(string Name);
        private sealed record ProjectReadDto(Guid Id, string Name, string Slug, string Role, byte[] RowVersion);

        private sealed record MemberReadDto(Guid ProjectId, Guid UserId, string Role, DateTimeOffset JoinedAtUtc, DateTimeOffset? RemovedAtUtc, byte[] RowVersion);
        private sealed record AddMemberDto(Guid UserId, ProjectRole Role, DateTimeOffset JoinedAtUtc);
        private sealed record ChangeMemberRoleDto(ProjectRole Role, byte[] RowVersion);
        private sealed record RemoveMemberDto(byte[] RowVersion, DateTimeOffset RemovedAtUtc);
        private sealed record RestoreMemberDto(byte[] RowVersion);

        [Fact]
        public async Task Admin_Adds_ChangesRole_Removes_And_Restores_Member()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            // owner creates project
            var owner = await RegisterAndLogin(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);
            (await client.PostAsJsonAsync("/projects", new ProjectCreateDto("Team"))).EnsureSuccessStatusCode();
            var prj = (await client.GetFromJsonAsync<List<ProjectReadDto>>("/projects", Json))!.Single(p => p.Name == "Team");

            // another user to be added
            var member = await RegisterAndLogin(client, "Non Default User Name");

            // add as Member (Requires ProjectAdmin -> owner qualifies)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);
            var add = await client.PostAsJsonAsync($"/projects/{prj.Id}/members", new AddMemberDto(member.UserId, ProjectRole.Member, DateTimeOffset.UtcNow));
            add.StatusCode.Should().Be(HttpStatusCode.Created);

            // list members (Reader or above; owner has it)
            var list = await client.GetAsync($"/projects/{prj.Id}/members?includeRemoved=false");
            list.StatusCode.Should().Be(HttpStatusCode.OK);
            var items = await list.Content.ReadFromJsonAsync<List<MemberReadDto>>(Json);
            var m = items!.Single(x => x.UserId == member.UserId);

            // change role to Admin (Requires ProjectOwner -> owner qualifies)
            var change = await client.PatchAsJsonAsync($"/projects/{prj.Id}/members/{member.UserId}/role", new ChangeMemberRoleDto(ProjectRole.Admin, m.RowVersion));
            change.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // refetch to get new RowVersion
            items = await client.GetFromJsonAsync<List<MemberReadDto>>($"/projects/{prj.Id}/members?includeRemoved=false", Json);
            m = items!.Single(x => x.UserId == member.UserId);

            // remove (Requires ProjectAdmin)
            var rem = await client.PatchAsJsonAsync($"/projects/{prj.Id}/members/{member.UserId}/remove", new RemoveMemberDto(m.RowVersion, DateTimeOffset.UtcNow));
            rem.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var listActive = await client.GetFromJsonAsync<List<MemberReadDto>>($"/projects/{prj.Id}/members?includeRemoved=false", Json);
            listActive!.Any(x => x.UserId == member.UserId).Should().BeFalse();

            var listRemoved = await client.GetFromJsonAsync<List<MemberReadDto>>($"/projects/{prj.Id}/members?includeRemoved=true", Json);
            listRemoved!.Any(x => x.UserId == member.UserId).Should().BeTrue();

            // restore (Requires ProjectAdmin)
            // need current RowVersion -> fetch includeRemoved
            var removed = listRemoved!.Single(x => x.UserId == member.UserId);
            var restore = await client.PatchAsJsonAsync($"/projects/{prj.Id}/members/{member.UserId}/restore", new RestoreMemberDto(removed.RowVersion));
            restore.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        private static async Task<AuthToken> RegisterAndLogin(HttpClient client, string userName = "Test User")
        {
            var email = $"{Guid.NewGuid():N}@demo.com";
            var name = userName;
            var password = "Str0ngP@ss!";
            (await client.PostAsJsonAsync("/auth/register", new RegisterReq(email, name, password))).EnsureSuccessStatusCode();
            var login = await client.PostAsJsonAsync("/auth/login", new { email, password });
            login.EnsureSuccessStatusCode();
            var dto = await login.Content.ReadFromJsonAsync<AuthToken>(Json);
            return dto!;
        }
    }
}
