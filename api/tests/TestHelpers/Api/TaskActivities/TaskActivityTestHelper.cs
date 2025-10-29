
namespace TestHelpers.Api.TaskActivities
{
    public static class TaskActivityTestHelper
    {

        // ----- GET ACTIVITIES -----

        public static async Task<HttpResponseMessage> GetTaskActivitiesResponseAsync(
            HttpClient client,
            Guid projectId,
            Guid laneId,
            Guid columnId,
            Guid taskId)
        {
            var response = await client.GetAsync(
                $"/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/activities");
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
