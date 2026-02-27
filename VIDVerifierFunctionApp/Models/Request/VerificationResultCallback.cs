using System.Text.Json.Serialization;

namespace VIDVerifier.Models.Request;

/// <summary>
/// Generic payload sent back to the caller when verification completes.
/// </summary>
public record VerificationResultCallback
{
    [JsonRequired]
    public required string RequestId { get; init; }

    [JsonRequired]
    public required string Status { get; init; }

    public string? Message { get; init; }

    public Dictionary<string, object>? FaceCheck { get; init; }
}
