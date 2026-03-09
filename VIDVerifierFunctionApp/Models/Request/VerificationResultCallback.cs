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

    public List<VerifiedCredentialResult>? VerifiedCredentials { get; init; }
}

public record VerifiedCredentialResult
{
    public string? Issuer { get; init; }

    public List<string>? Type { get; init; }

    public Dictionary<string, object>? Claims { get; init; }

    public FaceCheckResult? FaceCheck { get; init; }
}
