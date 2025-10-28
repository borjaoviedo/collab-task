using Application.Columns.DTOs;
using TestHelpers.Api.Defaults;
using TestHelpers.Api.Http;

namespace TestHelpers.Api.Columns
{
    public static class ColumnTestHelper
    {
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
            var response = await HttpRequestExtensions.PostWithoutIfMatchAsync(
                client,
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

        public static async Task<HttpResponseMessage> GetColumnsResponseAsync(HttpClient client, Guid projectId, Guid laneId)
        {
            var response = await client.GetAsync($"/projects/{projectId}/lanes/{laneId}/columns");
            return response;
        }

        public static async Task<List<ColumnReadDto>> GetColumnsDtoAsync(HttpClient client, Guid projectId, Guid laneId)
        {
            var response = await GetColumnsResponseAsync(client, projectId, laneId);
            var columns = await response.ReadContentAsDtoAsync<List<ColumnReadDto>>();

            return columns;
        }
    }
}
