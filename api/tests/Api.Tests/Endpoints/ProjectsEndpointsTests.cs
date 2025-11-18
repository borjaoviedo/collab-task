using Api.Tests.Testing;
using Application.Projects.DTOs;
using Application.Users.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using TestHelpers.Api.Common;
using TestHelpers.Api.Common.Http;
using TestHelpers.Api.Endpoints.Auth;
using TestHelpers.Api.Endpoints.Defaults;
using TestHelpers.Api.Endpoints.Projects;

namespace Api.Tests.Endpoints
{
    public sealed class ProjectsEndpointsTests
    {
        [Fact]
        public async Task Create_Then_List_Shows_Project_For_User()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);

            // Create default project
            await ProjectTestHelper.PostProjectDtoAsync(client);

            var projectList = await ProjectTestHelper.GetProjectsDtoAsync(client);

            projectList.Should().NotBeNull();
            projectList.Should().Contain(p => p.Name == ProjectDefaults.DefaultProjectName);
        }

        [Fact]
        public async Task Get_ById_Returns200_For_Owner()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);

            // Create default project
            var project = await ProjectTestHelper.PostProjectDtoAsync(client);

            // Get project by id
            var response = await ProjectTestHelper.GetProjectResponseAsync(client, project.Id);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Rename_Returns200_For_Admin_Or_Owner()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);

            // Create default project
            var project = await ProjectTestHelper.PostProjectDtoAsync(client);

            // Rename project with default rename DTO
            var renameResponse = await ProjectTestHelper.RenameProjectResponseAsync(
                client,
                project.Id,
                project.RowVersion);
            renameResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var renamed = await renameResponse.ReadContentAsDtoAsync<ProjectReadDto>();
            renamed.Name.Should().Be(ProjectDefaults.DefaultProjectRename);
        }

        [Fact]
        public async Task Delete_Returns204_For_Owner()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);

            // Create default project
            var project = await ProjectTestHelper.PostProjectDtoAsync(client);

            // Delete response: 204 no content
            var deleteResponse = await ProjectTestHelper.DeleteProjectResponseAsync(
                client,
                project.Id,
                project.RowVersion);
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Create_Then_List_And_GetById_Sends_ETag()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);

            // Create default project
            var project = await ProjectTestHelper.PostProjectDtoAsync(client);

            // List
            var getProjectsResponse = await ProjectTestHelper.GetProjectsResponseAsync(client);
            getProjectsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var projectList = await getProjectsResponse.ReadContentAsDtoAsync<List<ProjectReadDto>>();
            projectList.Should().ContainSingle(p => p.Name == ProjectDefaults.DefaultProjectName);

            // GetById -> ETag
            var getProjectResponse = await ProjectTestHelper.GetProjectResponseAsync(client, project.Id);
            getProjectResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            getProjectResponse.Headers.ETag.Should().NotBeNull();
        }

        [Fact]
        public async Task Get_ById_NotFound_For_Foreign_User()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);

            // Create default project as first user
            var project = await ProjectTestHelper.PostProjectDtoAsync(client);

            // Authenticate as a different user who is not a member of that project
            var userRegisterDto = new UserRegisterDto
            {
                Email = "random@e.com",
                Name = "random",
                Password = "Passw0rd!"
            };
            await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client, userRegisterDto);

            // Service hides foreign projects as 404 instead of 403
            var response = await ProjectTestHelper.GetProjectResponseAsync(client, project.Id);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Rename_Valid_Then_Stale_412_And_Missing_428()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);

            // Create default project
            var project = await ProjectTestHelper.PostProjectDtoAsync(client);

            // OK with valid If-Match
            var okRenameResponse = await ProjectTestHelper.RenameProjectResponseAsync(
                client,
                project.Id,
                project.RowVersion);
            okRenameResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var renamed = await okRenameResponse.ReadContentAsDtoAsync<ProjectReadDto>();

            // Stale 412
            var staleRowVersion = CommonApiTestHelpers.GenerateStaleRowVersion(renamed.RowVersion);
            var differentRenameDto = new ProjectRenameDto { NewName = "different rename" };
            var staleRenameResponse = await ProjectTestHelper.RenameProjectResponseAsync(
                client,
                project.Id,
                staleRowVersion,
                differentRenameDto);
            staleRenameResponse.StatusCode.Should().Be((HttpStatusCode)412);

            // Missing If-Match -> 428
            client.DefaultRequestHeaders.IfMatch.Clear();
            var missingRenameResponse = await client.PatchAsJsonAsync(
                $"/projects/{renamed.Id}/rename",
                differentRenameDto);
            missingRenameResponse.StatusCode.Should().Be((HttpStatusCode)428);
        }

        [Fact]
        public async Task Delete_204_With_IfMatch_And_428_When_Missing()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);

            // Create default project
            var project = await ProjectTestHelper.PostProjectDtoAsync(client);

            // 428 missing If-Match
            client.DefaultRequestHeaders.IfMatch.Clear();
            var missingDeleteResponse = await client.DeleteAsync($"/projects/{project.Id}");
            missingDeleteResponse.StatusCode.Should().Be((HttpStatusCode)428);

            // 204 with If-Match
            var deleteResponse = await ProjectTestHelper.DeleteProjectResponseAsync(
                client,
                project.Id,
                project.RowVersion);
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Me_200_And_ByUser_403()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var user = await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);

            // Create default project
            await ProjectTestHelper.PostProjectDtoAsync(client);

            // Create non default project
            var differentCreateDto = new ProjectCreateDto { Name = "PB" };
            await ProjectTestHelper.PostProjectDtoAsync(client, differentCreateDto);

            // /projects returns projects where the caller has at least reader rights
            var meResponse = await ProjectTestHelper.GetProjectsResponseAsync(client);
            meResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var myProjects = await meResponse.ReadContentAsDtoAsync<List<ProjectReadDto>>();
            myProjects.Should().NotBeNull();
            myProjects.Should().NotBeEmpty();

            // /projects/users/{id} requires SystemAdmin
            var byUserForbidden = await ProjectTestHelper.GetProjectsByUserResponseAsync(
                client,
                user.UserId);

            byUserForbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}
