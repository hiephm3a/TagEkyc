using System.Text.Json;

namespace TagEkyc.Infrastructure.Persistence;

internal static class PersistenceJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static T Deserialize<T>(string value) =>
        JsonSerializer.Deserialize<T>(value, Options) ?? throw new InvalidOperationException("Stored JSON could not be deserialized.");
}
