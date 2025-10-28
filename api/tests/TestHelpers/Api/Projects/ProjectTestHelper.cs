using Application.Lanes.DTOs;
using Application.Projects.DTOs;
using Application.TaskItems.DTOs;
using TestHelpers.Api.Http;

namespace TestHelpers.Api.Projects
{
    public static class ProjectTestHelper
    {
        public static async Task<ProjectReadDto> CreateProject(HttpClient client, string name = "Project")
        {
            var createDto = new ProjectCreateDto() { Name = name };
            var response = await HttpRequestExtensions.PostWithoutIfMatchAsync(
                client,
                "/projects",
                createDto);
            var project = await response.ReadContentAsDtoAsync<ProjectReadDto>();

            return project!;
        }

        public static async Task<LaneReadDto> CreateLane(
            HttpClient client,
            Guid projectId,
            string name = "Lane",
            int order = 0)
        {
            var createDto = new LaneCreateDto() { Name = name, Order = order };
            var response = await HttpRequestExtensions.PostWithoutIfMatchAsync(
                client,
                $"/projects/{projectId}/lanes",
                createDto);
            var lane = await response.ReadContentAsDtoAsync<LaneReadDto>();

            return lane!;
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
