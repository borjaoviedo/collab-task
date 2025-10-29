using Api.Tests.Testing;
using Application.ProjectMembers.DTOs;
using Application.Projects.DTOs;
using Domain.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using TestHelpers.Api.Auth;
using TestHelpers.Api.Defaults;
using TestHelpers.Api.Http;
using TestHelpers.Api.ProjectMembers;
using TestHelpers.Api.Projects;

namespace Api.Tests.Endpoints
{
    public sealed class ProjectMembersEndpointsTests
    {
        [Fact]
        public async Task Admin_Adds_ChangesRole_Removes_And_Restores_Member()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            // Owner creates project
            var owner = await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);
            var project = await ProjectTestHelper.PostProjectDtoAsync(client);

            // Another user to be added
            var user = await AuthTestHelper.PostRegisterAndLoginAsync(
                client,
                email: "random@e.com",
                name: "Non Default User Name");

            // Add as Member (requires ProjectAdmin -> owner qualifies)
            client.SetAuthorization(owner.AccessToken);
            var createProjectMemberDto = new ProjectMemberCreateDto()
            {
                Role = ProjectMemberDefaults.DefaultProjectMemberRole,
                UserId = user.UserId
            };
            var createResponse = await ProjectMemberTestHelper.PostProjectMemberResponseAsync(
                client,
                project.Id,
                createProjectMemberDto);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var projectMember = await ProjectMemberTestHelper.GetProjectMemberDtoAsync(
                client,
                project.Id,
                user.UserId);

            // Change role
            var changeRoleDto = await ProjectMemberTestHelper.ChangeProjectMemberRoleDtoAsync(
                client,
                project.Id,
                projectMember.UserId,
                projectMember.RowVersion);
            changeRoleDto.Role.Should().Be(ProjectRole.Admin);

            // Refetch to get new RowVersion
            projectMember = await ProjectMemberTestHelper.GetProjectMemberDtoAsync(client, project.Id, user.UserId);

            // Remove 
            var removedDto = await ProjectMemberTestHelper.RemoveProjectMemberDtoAsync(
                client,
                project.Id,
                projectMember.UserId,
                projectMember.RowVersion);
            removedDto.RemovedAt.Should().NotBeNull();

            // Restore
            var restoredDto = await ProjectMemberTestHelper.RestoreProjectMemberDtoAsync(client, project.Id, projectMember.UserId, removedDto.RowVersion);
            restoredDto.RemovedAt.Should().BeNull();
        }

        [Fact]
        public async Task Create_With_IfMatch_Header_Returns_400()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var owner = await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);
            var project = await ProjectTestHelper.PostProjectDtoAsync(client);
            var user = await AuthTestHelper.PostRegisterAndLoginAsync(
                client,
                email: "random@e.com",
                name: "Other user name");

            // Back to owner authorization
            client.SetAuthorization(owner.AccessToken);

            client.DefaultRequestHeaders.IfMatch.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", "\"abc\"");

            var createResponse = await client.PostAsJsonAsync(
                $"/projects/{project.Id}/members",
                new ProjectMemberCreateDto { UserId = user.UserId, Role = ProjectRole.Member });
            createResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ChangeRole_Valid_Then_Stale_Then_Missing_IfMatch()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var owner = await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);
            var project = await ProjectTestHelper.PostProjectDtoAsync(client);

            var user = await AuthTestHelper.PostRegisterAndLoginAsync(
                client,
                email: "random@e.com",
                name: "Other user name");

            // Add user to project
            client.SetAuthorization(owner.AccessToken);
            var createDto = new ProjectMemberCreateDto()
            {
                Role = ProjectMemberDefaults.DefaultProjectMemberRole,
                UserId = user.UserId
            };
            await ProjectMemberTestHelper.PostProjectMemberResponseAsync(
                client,
                project.Id,
                createDto);

            // Get project member to get rowversion
            var member = await ProjectMemberTestHelper.GetProjectMemberDtoAsync(
                client,
                project.Id,
                user.UserId);

            // valid change role
            var okChangeRoleResponse = await ProjectMemberTestHelper.ChangeProjectMemberRoleResponseAsync(
                client,
                project.Id,
                user.UserId,
                member.RowVersion);
            okChangeRoleResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // stale change role
            var differentChangeRoleDto = new ProjectMemberChangeRoleDto { NewRole = ProjectRole.Member };
            var staleChangeRoleResponse = await ProjectMemberTestHelper.ChangeProjectMemberRoleResponseAsync(
                client,
                project.Id,
                user.UserId,
                member.RowVersion, // old rowVersion
                differentChangeRoleDto);
            staleChangeRoleResponse.StatusCode.Should().Be((HttpStatusCode)412);

            // missing If-Match -> 428
            client.DefaultRequestHeaders.IfMatch.Clear();
            var missingChangeRoleResponse = await client.PatchAsJsonAsync(
                $"/projects/{project.Id}/members/{user.UserId}/role",
                differentChangeRoleDto);
            missingChangeRoleResponse.StatusCode.Should().Be((HttpStatusCode)428);
        }

        [Fact]
        public async Task Remove_Then_Restore_With_Concurrency()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var owner = await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);
            var project = await ProjectTestHelper.PostProjectDtoAsync(client);
            var user = await AuthTestHelper.PostRegisterAndLoginAsync(
                client,
                email: "random@e.com",
                name: "Other user name");

            // Add member
            client.SetAuthorization(owner.AccessToken);
            var createDto = new ProjectMemberCreateDto()
            {
                Role = ProjectMemberDefaults.DefaultProjectMemberRole,
                UserId = user.UserId
            };
            var member = await ProjectMemberTestHelper.PostProjectMemberDtoAsync(
                client,
                project.Id,
                createDto);

            // Remove
            var removeResponse = await ProjectMemberTestHelper.RemoveProjectMemberResponseAsync(
                client,
                project.Id,
                member.UserId,
                member.RowVersion);
            removeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Refetch to restore with correct rowVersion
            member = await ProjectMemberTestHelper.GetProjectMemberDtoAsync(client, project.Id, member.UserId);

            // Restore
            var restoreResponse = await ProjectMemberTestHelper.RestoreProjectMemberResponseAsync(
                client,
                project.Id,
                member.UserId,
                member.RowVersion);
            restoreResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetById_404_Vs_403_And_GetRole_404()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            // user A
            var userA = await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);
            var project = await ProjectTestHelper.PostProjectDtoAsync(client);

            // 404: member not in project
            var notFoundResponse = await ProjectMemberTestHelper.GetProjectMemberResponseAsync(
                client,
                project.Id,
                userId: Guid.NewGuid());
            notFoundResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

            // user B tries to access A's project -> 403
            var userB = await AuthTestHelper.PostRegisterAndLoginAsync(
                client,
                name: "userB",
                email: "b@x.com");
            client.SetAuthorization(userB.AccessToken);

            var forbiddenResponse = await ProjectMemberTestHelper.GetProjectMemberResponseAsync(
                client,
                project.Id,
                userId: Guid.NewGuid());
            forbiddenResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            // Get role 404 (non-member)
            client.SetAuthorization(userA.AccessToken);
            var notFoundMemberResponse = await ProjectMemberTestHelper.ChangeProjectMemberRoleResponseAsync(
                client,
                project.Id,
                userId: Guid.NewGuid(),
                rowVersion: [1, 2, 3]);
            notFoundMemberResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Count_Me_200_Unauthorized_401_And_Admin_ByUser_403()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            // Unauthorized
            var unauth = await ProjectMemberTestHelper.GetProjectMemberMeCountResponseAsync(client);
            unauth.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            // Login normal user
            var user = await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);

            // Create two projects to own
            await ProjectTestHelper.PostProjectDtoAsync(client);
            var differentCreateDto = new ProjectCreateDto() { Name = "different project" };
            await ProjectTestHelper.PostProjectDtoAsync(client, differentCreateDto);

            var me = await ProjectMemberTestHelper.GetProjectMemberMeCountResponseAsync(client);
            me.StatusCode.Should().Be(HttpStatusCode.OK);
            var meCount = await me.Content.ReadFromJsonAsync<ProjectMemberCountReadDto>();
            meCount!.Count.Should().BeGreaterThanOrEqualTo(1);

            // Admin endpoint without admin -> 403
            var byUserForbidden = await client.GetAsync($"/members/{user.UserId}/count");
            byUserForbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}
