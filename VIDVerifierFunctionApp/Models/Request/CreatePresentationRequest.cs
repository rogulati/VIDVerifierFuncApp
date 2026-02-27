using System.Text.Json.Serialization;

namespace VIDVerifier.Models.Request;

public record CreatePresentationRequest
{
    [JsonRequired]
    public required string Authority { get; init; }

    [JsonRequired]
    public required Dictionary<string, object> Registration { get; init; }

    [JsonRequired]
    public required List<Dictionary<string, object>> RequestedCredentials { get; init; }

    [JsonPropertyName("includeQRCode")]
    public bool IncludeQrCode { get; set; }

    public bool IncludeReceipt { get; set; }

    public Callback? Callback { get; set; }
}
