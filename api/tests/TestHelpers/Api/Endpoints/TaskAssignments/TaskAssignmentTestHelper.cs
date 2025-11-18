using Application.TaskAssignments.DTOs;
using TestHelpers.Api.Http;

namespace TestHelpers.Api.Endpoints.TaskAssignments
{
    public static class TaskAssignmentTestHelper
    {
        // ----- POST -----

        public static async Task<HttpResponseMessage> PostAssignmentResponseAsync(
            HttpClient client,
            Guid projectId,
            Guid taskId,
            TaskAssignmentCreateDto dto)
        {
            var response = await client.PostWithoutIfMatchAsync(
                $"/projects/{projectId}/tasks/{taskId}/assignments",
                dto);

            return response;
        }

        // ----- GET ASSIGNMENT BY ID -----

        public static async Task<HttpResponseMessage> GetAssignmentByIdResponseAsync(
            HttpClient client,
            Guid projectId,
            Guid taskId,
            Guid userId)
        {
            var response = await client.GetAsync(
                $"/projects/{projectId}/tasks/{taskId}/assignments/{userId}");
            return response;
        }

        // ----- GET ASSIGNMENT BY USER -----

        public static async Task<HttpResponseMessage> GetAssignmentByUserResponseAsync(
            HttpClient client,
            Guid userId)
        {
            var response = await client.GetAsync($"/assignments/users/{userId}");
            return response;
        }

        // ----- DELETE -----

        public static async Task<HttpResponseMessage> DeleteAssignmentResponseAsync(
            HttpClient client,
            Guid projectId,
            Guid taskId,
            Guid userId,
            string rowVersion)
        {
            var deleteResponse = await client.DeleteWithIfMatchAsync(
                rowVersion,
                $"/projects/{projectId}/tasks/{taskId}/assignments/{userId}");

            return deleteResponse;
        }
    }
}
