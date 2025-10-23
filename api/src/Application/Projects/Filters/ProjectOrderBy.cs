using System.Text.Json.Serialization;

namespace Application.Projects.Filters
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ProjectOrderBy
    {
        CreatedAtAsc,
        CreatedAtDesc,
        NameAsc,
        NameDesc,
        UpdatedAtAsc,
        UpdatedAtDesc
    }
}
