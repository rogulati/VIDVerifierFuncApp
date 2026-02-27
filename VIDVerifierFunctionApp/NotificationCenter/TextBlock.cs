using System.Text.Json.Serialization;

namespace VIDVerifier.NotificationCenter;

public record TextBlock : Block
{
    [JsonRequired]
    public required string Text { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Weight { get; init; }
}
