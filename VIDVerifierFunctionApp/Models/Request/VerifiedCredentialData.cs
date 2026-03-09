using System.Text.Json.Serialization;

namespace VIDVerifier.Models.Request;

public record VerifiedCredentialData
{
    public string? Issuer { get; init; }

    public List<string>? Type { get; init; }

    public Dictionary<string, object>? Claims { get; init; }

    public FaceCheckResult? FaceCheck { get; init; }
}

public record FaceCheckResult
{
    public double? MatchConfidenceScore { get; init; }
}
