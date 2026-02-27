using System.Text.Json.Serialization;

namespace VIDVerifier.NotificationCenter;

public record Attachment
{
    [JsonRequired]
    public required string ContentType { get; init; }

    [JsonRequired]
    public required AttachmentContent Content { get; init; }
}
