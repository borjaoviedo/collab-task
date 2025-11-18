using Application.TaskItems.DTOs;
using TestHelpers.Api.Common.Http;
using TestHelpers.Api.Endpoints.Defaults;

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

        // ----- GET TASKS BY COLUMN -----

        public static async Task<HttpResponseMessage> GetTaskItemsByColumnResponseAsync(
            HttpClient client,
            Guid projectId,
            Guid columnId)
        {
            var response = await client.GetAsync($"/projects/{projectId}/columns/{columnId}/tasks");
            return response;
        }
    }
}
