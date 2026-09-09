using System.Text.Json;
using System.Text.Json.Serialization;

namespace DockManager.Core.Persistence;

/// <summary>
/// Shared serializer configuration for the dock's JSON documents. One instance is reused for every
/// read and write: creating options per call is one of the classic System.Text.Json slow paths.
/// </summary>
internal static class JsonFile
{
    public static JsonSerializerOptions Options { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        // The specific converter is registered first so it wins over the generic enum factory.
        options.Converters.Add(new PinnedItemKindConverter());
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
