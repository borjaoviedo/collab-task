using Application.Lanes.DTOs;
using TestHelpers.Api.Defaults;
using TestHelpers.Api.Http;

namespace TestHelpers.Api.Lanes
{
    public static class LaneTestHelper
    {

        // ----- POST -----

        public static async Task<HttpResponseMessage> PostLaneResponseAsync(
            HttpClient client,
            Guid projectId,
            LaneCreateDto? dto = null)
        {
            string name;
            int order;

            if (dto is null)
            {
                name = LaneDefaults.DefaultLaneName;
                order = LaneDefaults.DefaultLaneOrder;
            }
            else
            {
                name = dto.Name;
                order = dto.Order;
            }

            var createDto = new LaneCreateDto() { Name = name, Order = order };
            var response = await client.PostWithoutIfMatchAsync(
                $"/projects/{projectId}/lanes",
                createDto);

            return response;
        }

        public static async Task<LaneReadDto> PostLaneDtoAsync(
            HttpClient client,
            Guid projectId,
            LaneCreateDto? dto = null)
        {
            var response = await PostLaneResponseAsync(client, projectId, dto);
            var lane = await response.ReadContentAsDtoAsync<LaneReadDto>();

            return lane;
        }


        // ----- GET LANES -----

        public static async Task<HttpResponseMessage> GetLanesResponseAsync(
            HttpClient client,
            Guid projectId)
        {
            var response = await client.GetAsync($"/projects/{projectId}/lanes");
            return response;
        }

        // ----- GET LANE -----

        public static async Task<HttpResponseMessage> GetLaneResponseAsync(
            HttpClient client,
            Guid projectId,
            Guid laneId)
        {
            var response = await client.GetAsync($"/projects/{projectId}/lanes/{laneId}");
            return response;
        }

        // ----- PUT RENAME -----

        public static async Task<HttpResponseMessage> RenameLaneResponseAsync(
            HttpClient client,
            Guid projectId,
            Guid laneId,
            string rowVersion,
            LaneRenameDto? dto = null)
        {
            var newName = dto is null ? LaneDefaults.DefaultLaneRename : dto.NewName;
            var renameDto = new LaneRenameDto() { NewName = newName };

            var renameResponse = await client.PatchWithIfMatchAsync(
                rowVersion,
                $"/projects/{projectId}/lanes/{laneId}/rename",
                renameDto);

            return renameResponse;
        }

        public static async Task<LaneReadDto> RenameLaneDtoAsync(
            HttpClient client,
            Guid projectId,
            Guid laneId,
            string rowVersion,
            LaneRenameDto? dto = null)
        {
            var response = await RenameLaneResponseAsync(client, projectId, laneId, rowVersion, dto);
            var lane = await response.ReadContentAsDtoAsync<LaneReadDto>();

            return lane;
        }

        // ----- PUT REORDER -----

        public static async Task<HttpResponseMessage> ReorderLaneResponseAsync(
            HttpClient client,
            Guid projectId,
            Guid laneId,
            string rowVersion,
            LaneReorderDto? dto = null)
        {
            var newOrder = dto is null ? LaneDefaults.DefaultLaneReorder : dto.NewOrder;
            var reorderDto = new LaneReorderDto() { NewOrder = newOrder };

            var reorderResponse = await client.PatchWithIfMatchAsync(
                rowVersion,
                $"/projects/{projectId}/lanes/{laneId}/reorder",
                reorderDto);

            return reorderResponse;
        }

        public static async Task<LaneReadDto> ReorderLaneDtoAsync(
            HttpClient client,
            Guid projectId,
            Guid laneId,
            string rowVersion,
            LaneReorderDto? dto = null)
        {
            var response = await ReorderLaneResponseAsync(client, projectId, laneId, rowVersion, dto);
            var lane = await response.ReadContentAsDtoAsync<LaneReadDto>();

            return lane;
        }

        // ----- DELETE -----

        public static async Task<HttpResponseMessage> DeleteLaneResponseAsync(
            HttpClient client,
            Guid projectId,
            Guid laneId,
            string rowVersion)
        {
            var deleteResponse = await client.DeleteWithIfMatchAsync(
                rowVersion,
                $"/projects/{projectId}/lanes/{laneId}");

            return deleteResponse;
        }
    }
}
