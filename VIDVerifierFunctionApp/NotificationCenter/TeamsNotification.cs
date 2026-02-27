using System.Text.Json;
using System.Text.Json.Serialization;

namespace VIDVerifier.NotificationCenter;

public class TeamsNotification(string type) : INotification
{
    [JsonIgnore]
    public string Id { get; init; } = Guid.NewGuid().ToString();

    public string Type { get; } = type;

    public List<Attachment> Attachments { get; } = [];

    public void AddAttachment(Attachment attachment)
    {
        Attachments.Add(attachment);
    }

    public void AddAttachment(AttachmentBuilder attachmentBuilder)
    {
        this.AddAttachment(attachmentBuilder.Build());
    }

    public string ToJson(JsonSerializerOptions jsonSerializerOptions)
    {
        return JsonSerializer.Serialize(this, jsonSerializerOptions);
    }
}
