using System.Text.Json.Serialization;

namespace VIDVerifier.Models.Request;

public record PresentationCallbackRequest
{
    [JsonRequired]
    public required string RequestStatus { get; init; }

    [JsonRequired]
    public required Guid RequestId { get; init; }

    [JsonRequired]
    public required string State { get; init; }

    public Dictionary<string, object>? FaceCheck { get; init; }
}
