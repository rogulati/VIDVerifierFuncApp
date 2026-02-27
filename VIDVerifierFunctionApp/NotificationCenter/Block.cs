using System.Text.Json.Serialization;

namespace VIDVerifier.NotificationCenter;

[JsonDerivedType(typeof(ImageBlock))]
[JsonDerivedType(typeof(TextBlock))]
public abstract record Block
{
    [JsonRequired]
    public required string Type { get; init; }

    public string? Size { get; init; }
}
