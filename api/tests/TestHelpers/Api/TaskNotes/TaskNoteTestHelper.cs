using Application.TaskNotes.DTOs;
using TestHelpers.Api.Defaults;
using TestHelpers.Api.Http;

namespace TestHelpers.Api.TaskNotes
{
    public static class TaskNoteTestHelper
    {

        // ----- GET NOTES -----

        public static async Task<HttpResponseMessage> GetTaskNotesResponseAsync(
            HttpClient client,
            Guid projectId,
            Guid laneId,
            Guid columnId,
            Guid taskId)
        {
            var response = await client.GetAsync(
                $"/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes");
            return response;
        }

        // ----- GET NOTE BY ID -----

        public static async Task<HttpResponseMessage> GetTaskNoteByIdResponseAsync(
            HttpClient client,
            Guid projectId,
            Guid laneId,
            Guid columnId,
            Guid taskId,
            Guid noteId)
        {
            var response = await client.GetAsync(
                $"/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes/{noteId}");
            return response;
        }

        // ----- GET NOTES BY USER -----

        public static async Task<HttpResponseMessage> GetTaskNotesByUserResponseAsync(
            HttpClient client,
            Guid projectId,
            Guid userId)
        {
            var response = await client.GetAsync($"/projects/{projectId}/notes/users/{userId}");
            return response;
        }

        // ----- POST -----

        public static async Task<HttpResponseMessage> PostNoteResponseAsync(
            HttpClient client,
            Guid projectId,
            Guid laneId,
            Guid columnId,
            Guid taskId,
            TaskNoteCreateDto? dto = null)
        {
            var content = dto is null ? TaskNoteDefaults.DefaultNoteContent : dto.Content;
            var createDto = new TaskNoteCreateDto() { Content = content };

            var response = await client.PostWithoutIfMatchAsync(
                $"/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes",
                createDto);

            return response;
        }

        public static async Task<TaskNoteReadDto> PostNoteDtoAsync(
            HttpClient client,
            Guid projectId,
            Guid laneId,
            Guid columnId,
            Guid taskId,
            TaskNoteCreateDto? dto = null)
        {
            var response = await PostNoteResponseAsync(
                client,
                projectId,
                laneId,
                columnId,
                taskId,
                dto);
            var note = await response.ReadContentAsDtoAsync<TaskNoteReadDto>();

            return note;
        }

        // ----- PUT EDIT -----

        public static async Task<HttpResponseMessage> EditNoteResponseAsync(
            HttpClient client,
            Guid projectId,
            Guid laneId,
            Guid columnId,
            Guid taskId,
            Guid noteId,
            byte[] rowVersion,
            TaskNoteEditDto? dto = null)
        {
            var newContent = dto is null ? TaskNoteDefaults.DefaultNoteNewContent : dto.NewContent;
            var editDto = new TaskNoteEditDto() { NewContent = newContent };

            var editResponse = await client.PatchWithIfMatchAsync(
                rowVersion,
                $"/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes/{noteId}/edit",
                editDto);

            return editResponse;
        }

        // ----- DELETE -----

        public static async Task<HttpResponseMessage> DeleteNoteResponseAsync(
            HttpClient client,
            Guid projectId,
            Guid laneId,
            Guid columnId,
            Guid taskId,
            Guid noteId,
            byte[] rowVersion)
        {
            var deleteResponse = await client.DeleteWithIfMatchAsync(
                rowVersion,
                $"/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes/{noteId}");

            return deleteResponse;
        }
    }
}
