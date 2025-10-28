using Application.Projects.DTOs;
using Application.TaskItems.DTOs;
using TestHelpers.Api.Defaults;
using TestHelpers.Api.Http;

namespace TestHelpers.Api.Projects
{
    public static class ProjectTestHelper
    {

        // ----- POST -----

        public static async Task<HttpResponseMessage> PostProjectResponseAsync(HttpClient client, ProjectCreateDto? dto = null)
        {
            var name = dto is null ? ProjectDefaults.DefaultProjectName : dto.Name;
            var createDto = new ProjectCreateDto() { Name = name };
            var response = await HttpRequestExtensions.PostWithoutIfMatchAsync(client, $"/projects", createDto);

            return response;
        }

        public static async Task<ProjectReadDto> PostProjectDtoAsync(HttpClient client, ProjectCreateDto? dto = null)
        {
            var response = await PostProjectResponseAsync(client, dto);
            var project = await response.ReadContentAsDtoAsync<ProjectReadDto>();

            return project;
        }

        // ----- GET PROJECTS -----

        public static async Task<HttpResponseMessage> GetProjectsResponseAsync(HttpClient client)
        {
            var response = await client.GetAsync($"/projects");
            return response;
        }

        public static async Task<List<ProjectReadDto>> GetProjectsDtoAsync(HttpClient client)
        {
            var response = await GetProjectsResponseAsync(client);
            var projects = await response.ReadContentAsDtoAsync<List<ProjectReadDto>>();

            return projects;
        }

        // ----- GET PROJECT -----

        public static async Task<HttpResponseMessage> GetProjectResponseAsync(HttpClient client, Guid projectId)
        {
            var response = await client.GetAsync($"/projects/{projectId}");
            return response;
        }

        // ----- PUT RENAME -----

        public static async Task<HttpResponseMessage> RenameProjectResponseAsync(
            HttpClient client,
            Guid projectId,
            byte[] rowVersion,
            ProjectRenameDto? dto = null)
        {
            var newName = dto is null ? ProjectDefaults.DefaultProjectRename : dto.NewName;
            var renameDto = new ProjectRenameDto() { NewName = newName };

            var renameResponse = await HttpRequestExtensions.PutWithIfMatchAsync(
                client,
                rowVersion,
                $"/projects/{projectId}/rename",
                renameDto);

            return renameResponse;
        }

        // ----- DELETE -----

        public static async Task<HttpResponseMessage> DeleteProjectResponseAsync(
            HttpClient client,
            Guid projectId,
            byte[] rowVersion)
        {
            var deleteResponse = await HttpRequestExtensions.DeleteWithIfMatchAsync(
                client,
                rowVersion,
                $"/projects/{projectId}");

            return deleteResponse;
        }


        public static async Task<TaskItemReadDto> CreateTask(
            HttpClient client,
            Guid projectId,
            Guid laneId,
            Guid columnId,
            string title = "Task Title",
            string description = "Task Description",
            DateTimeOffset? dueDate = null,
            decimal sortKey = 0m)
        {
            var createDto = new TaskItemCreateDto { Title = title, Description = description, DueDate = dueDate, SortKey = sortKey };
            var response = await HttpRequestExtensions.PostWithoutIfMatchAsync(
                client,
                $"/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks",
                createDto);
            var task = await response.ReadContentAsDtoAsync<TaskItemReadDto>();

            return task!;
        }

    }
}
