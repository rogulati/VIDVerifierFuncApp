using System.Text.Json.Serialization;

namespace VIDVerifier.Models.Response;

public record CreateRequestResponse
{
    [JsonRequired]
    public required Guid RequestId { get; init; }

    [JsonRequired]
    public required string Url { get; init; }

    [JsonPropertyName("expiry")]
    public int? Expiration { get; set; }

    public string? QrCode { get; set; }
}
