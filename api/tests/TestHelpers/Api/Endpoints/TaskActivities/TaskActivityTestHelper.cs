namespace TestHelpers.Api.Endpoints.TaskActivities
{
    public static class TaskActivityTestHelper
    {

        // ----- GET ACTIVITIES -----

        public static async Task<HttpResponseMessage> GetTaskActivitiesResponseAsync(
            HttpClient client,
            Guid projectId,
            Guid taskId)
        {
            var response = await client.GetAsync(
                $"/projects/{projectId}/tasks/{taskId}/activities");
            return response;
        }

        // ----- GET ACTIVTIES BY USER -----

        public static async Task<HttpResponseMessage> GetTaskActivitiesByUserResponseAsync(
            HttpClient client,
            Guid userId)
        {
            var response = await client.GetAsync($"/activities/users/{userId}");
            return response;
        }
    }
}
