using Application.Lanes.DTOs;

namespace TestHelpers.Api.Endpoints.Defaults
{
    public static class LaneDefaults
    {
        public readonly static string DefaultLaneName = "lane";
        public readonly static string DefaultLaneRename = "different name";
        public readonly static int DefaultLaneOrder = 0;
        public readonly static int DefaultLaneReorder = 1;

        public readonly static LaneCreateDto DefaultLaneCreateDto = new()
        {
            Name = DefaultLaneName,
            Order = DefaultLaneOrder
        };

        public readonly static LaneRenameDto DefaultLaneRenameDto = new() { NewName = DefaultLaneRename };

        public readonly static LaneReorderDto DefaultLaneReorderDto = new() { NewOrder = DefaultLaneReorder };
    }
}
