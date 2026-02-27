using System.Text.Json.Serialization;

namespace VIDVerifier.Models.Request;

public record StartFlowRequest
{
    /// <summary>
    /// Correlation ID passed through to the caller's callback.
    /// </summary>
    [JsonRequired]
    public required string State { get; init; }

    /// <summary>
    /// URL the Function App will POST verification results to.
    /// </summary>
    [JsonRequired]
    public required string CallerCallbackUrl { get; init; }

    /// <summary>
    /// Display name shown in the Verified ID presentation request and Teams notifications.
    /// For example "Role activation" or "B2C sign-in".
    /// </summary>
    public string? CallerName { get; init; }

    /// <summary>
    /// The Verified ID credential type to request (e.g. "VerifiedEmployee").
    /// Defaults to "VerifiedEmployee" when not provided.
    /// </summary>
    public string? CredentialType { get; init; }

    /// <summary>
    /// Whether to require FaceCheck validation. Defaults to true when not provided.
    /// </summary>
    public bool? RequireFaceCheck { get; init; }
}
