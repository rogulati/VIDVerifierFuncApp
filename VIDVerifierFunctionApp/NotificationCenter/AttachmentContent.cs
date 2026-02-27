
using System.Text.Json.Serialization;

namespace VIDVerifier.NotificationCenter;

public record AttachmentContent
{
    [JsonRequired]
    public required string Type { get; init; }

    [JsonRequired]
    public required List<Block> Body { get; init; }

    [JsonPropertyName("$schema")]
    [JsonRequired]
    public required string Schema { get; init; }

    [JsonRequired]
    public required string Version { get; init; }
}
