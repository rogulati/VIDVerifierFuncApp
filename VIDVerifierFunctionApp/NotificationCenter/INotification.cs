using System.Text.Json;

namespace VIDVerifier.NotificationCenter;

public interface INotification
{
    public string Id { get; init; }

    public string ToJson(JsonSerializerOptions jsonSerializerOptions);
}
