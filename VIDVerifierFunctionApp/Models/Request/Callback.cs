using System.Text.Json.Serialization;

namespace VIDVerifier.Models.Request;

public record Callback
{
    [JsonRequired]
    public required string Url { get; init; }

    [JsonRequired]
    public required string State { get; init; }
}
