using System.Text.Json;
using System.Text.Json.Serialization;

namespace JobScheduler.Dal.Common;

public static class Json
{
    public static JsonSerializerOptions Default { get; } = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() },
    };
}
