using System.Text.Json;
using System.Text.Json.Serialization;
using DockManager.Core.Items;

namespace DockManager.Core.Persistence;

/// <summary>
/// Tolerant kind converter: an unknown kind written by a future version degrades to a file entry
/// instead of making the whole pinned list unreadable.
/// </summary>
public sealed class PinnedItemKindConverter : JsonConverter<PinnedItemKind>
{
    public override PinnedItemKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                var text = reader.GetString();
                return Enum.TryParse<PinnedItemKind>(text, ignoreCase: true, out var parsed)
                    ? parsed
                    : PinnedItemKind.File;

            case JsonTokenType.Number:
                return reader.TryGetInt32(out var number) && Enum.IsDefined((PinnedItemKind)number)
                    ? (PinnedItemKind)number
                    : PinnedItemKind.File;

            default:
                return PinnedItemKind.File;
        }
    }

    public override void Write(Utf8JsonWriter writer, PinnedItemKind value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
