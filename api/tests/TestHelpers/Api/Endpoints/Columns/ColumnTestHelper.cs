using Application.Columns.DTOs;
using TestHelpers.Api.Common.Http;
using TestHelpers.Api.Endpoints.Defaults;

namespace TestHelpers.Api.Endpoints.Columns
{
    public static class ColumnTestHelper
    {

        // ----- POST -----

        public static async Task<HttpResponseMessage> PostColumnResponseAsync(
            HttpClient client,
            Guid projectId,
            Guid laneId,
            ColumnCreateDto? dto = null)
        {
            string name;
            int order;

            if (dto is null)
            {
                name = ColumnDefaults.DefaultColumnName;
                order = ColumnDefaults.DefaultColumnOrder;
            }
            else
            {
                name = dto.Name;
                order = dto.Order;
            }

            var createDto = new ColumnCreateDto() { Name = name, Order = order };
            var response = await client.PostWithoutIfMatchAsync(
                $"/projects/{projectId}/lanes/{laneId}/columns",
                createDto);

            return response;
        }

        public static async Task<ColumnReadDto> PostColumnDtoAsync(
            HttpClient client,
            Guid projectId,
            Guid laneId,
            ColumnCreateDto? dto = null)
        {
            var response = await PostColumnResponseAsync(client, projectId, laneId, dto);
            var column = await response.ReadContentAsDtoAsync<ColumnReadDto>();

            return column;
        }


        // ----- GET COLUMNS -----

        public static async Task<HttpResponseMessage> GetColumnsResponseAsync(
            HttpClient client,
            Guid projectId,
            Guid laneId)
        {
            var response = await client.GetAsync($"/projects/{projectId}/lanes/{laneId}/columns");
            return response;
        }

        public static async Task<List<ColumnReadDto>> GetColumnsDtoAsync(
            HttpClient client,
            Guid projectId,
            Guid laneId)
        {
            var response = await GetColumnsResponseAsync(client, projectId, laneId);
            var columns = await response.ReadContentAsDtoAsync<List<ColumnReadDto>>();

            return columns;
        }

        // ----- GET COLUMN -----

        public static async Task<HttpResponseMessage> GetColumnResponseAsync(
            HttpClient client,
            Guid projectId,
            Guid laneId,
            Guid columnId)
        {
            var response = await client.GetAsync($"/projects/{projectId}/lanes/{laneId}/columns/{columnId}");
            return response;
        }

        // ----- PUT RENAME -----

        public static async Task<HttpResponseMessage> RenameColumnResponseAsync(
            HttpClient client,
            Guid projectId,
            Guid laneId,
            Guid columnId,
            string rowVersion,
            ColumnRenameDto? dto = null)
        {
            var newName = dto is null ? ColumnDefaults.DefaultColumnRename : dto.NewName;
            var renameDto = new ColumnRenameDto() { NewName = newName };

            var renameResponse = await client.PatchWithIfMatchAsync(
                rowVersion,
                $"/projects/{projectId}/lanes/{laneId}/columns/{columnId}/rename",
                renameDto);

            return renameResponse;
        }

        // ----- PUT REORDER -----

        public static async Task<HttpResponseMessage> ReorderColumnResponseAsync(
            HttpClient client,
            Guid projectId,
            Guid laneId,
            Guid columnId,
            string rowVersion,
            ColumnReorderDto? dto = null)
        {
            var newOrder = dto is null ? ColumnDefaults.DefaultColumnReorder : dto.NewOrder;
            var reorderDto = new ColumnReorderDto() { NewOrder = newOrder };

            var reorderResponse = await client.PatchWithIfMatchAsync(
                rowVersion,
                $"/projects/{projectId}/lanes/{laneId}/columns/{columnId}/reorder",
                reorderDto);

            return reorderResponse;
        }

        // ----- DELETE -----

        public static async Task<HttpResponseMessage> DeleteColumnResponseAsync(
            HttpClient client,
            Guid projectId,
            Guid laneId,
            Guid columnId,
            string rowVersion)
        {
            var deleteResponse = await client.DeleteWithIfMatchAsync(
                rowVersion,
                $"/projects/{projectId}/lanes/{laneId}/columns/{columnId}");

            return deleteResponse;
        }
    }
}
