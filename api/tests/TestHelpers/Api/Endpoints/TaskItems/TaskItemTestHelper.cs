using Application.TaskItems.DTOs;
using TestHelpers.Api.Endpoints.Defaults;
using TestHelpers.Api.Http;

namespace TestHelpers.Api.Endpoints.TaskItems
{
    public static class TaskItemTestHelper
    {
        // ----- POST -----

        public static async Task<HttpResponseMessage> PostTaskItemResponseAsync(
            HttpClient client,
            Guid projectId,
            Guid laneId,
            Guid columnId,
            TaskItemCreateDto? dto = null)
        {
            string title, description;
            DateTimeOffset? dueDate;
            decimal sortKey;

            if (dto is null)
            {
                title = TaskItemDefaults.DefaultTaskTitle;
                description = TaskItemDefaults.DefaultTaskDescription;
                dueDate = TaskItemDefaults.DefaultDueDate;
                sortKey = TaskItemDefaults.DefaultSortKey;
            }
            else
            {
                title = dto.Title;
                description = dto.Description;
                dueDate = dto.DueDate;
                sortKey = dto.SortKey;
            }

            var createDto = new TaskItemCreateDto
            {
                Title = title,
                Description = description,
                DueDate = dueDate,
                SortKey = sortKey
            };
            var response = await client.PostWithoutIfMatchAsync(
                $"/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks",
                createDto);

            return response;
        }

        public static async Task<TaskItemReadDto> PostTaskItemDtoAsync(
            HttpClient client,
            Guid projectId,
            Guid laneId,
            Guid columnId,
            TaskItemCreateDto? dto = null)
        {
            var response = await PostTaskItemResponseAsync(client, projectId, laneId, columnId, dto);
            var task = await response.ReadContentAsDtoAsync<TaskItemReadDto>();

            return task;
        }

        // ----- GET TASKS -----

        public static async Task<HttpResponseMessage> GetTaskItemsResponseAsync(
            HttpClient client,
            Guid projectId)
        {
            var response = await client.GetAsync($"/projects/{projectId}/tasks");

            return response;
        }
    }
}
