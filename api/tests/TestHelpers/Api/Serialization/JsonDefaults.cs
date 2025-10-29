using System.Text.Json;
using System.Text.Json.Serialization;

namespace TestHelpers.Api.Serialization
{
    public static class JsonDefaults
    {
        public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };
    }
}
