using System.Text.Json.Serialization;

namespace VIDVerifier.NotificationCenter;

public record ImageBlock : Block
{
    [JsonRequired]
    public required string Url { get; init; }

    public string? AltText { get; init; }
}
