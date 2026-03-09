using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;

namespace VIDVerifier.NotificationCenter;

public class GenericWebhookNotificationCenter(
    Configuration configuration,
    IHttpClientFactory httpClientFactory,
    JsonSerializerOptions jsonSerializerOptions,
    ILogger<GenericWebhookNotificationCenter> logger)
        : INotificationCenter
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();

    public void SendNotification(INotification notification)
    {
        logger.LogInformation("Sending generic webhook notification with ID '{NotificationId}'.", notification.Id);

        _ = PostNotification(notification).ContinueWith(t =>
        {
            if (t.IsCompletedSuccessfully)
            {
                logger.LogInformation("Successfully sent notification '{NotificationId}'.", notification.Id);
            }
            else
            {
                logger.LogWarning(t.Exception, "Failed to send notification '{NotificationId}'.", notification.Id);
            }
        });
    }

    public void SendPresentationPendingNotification(Guid requestId, string qrCode, string callerName)
    {
        logger.LogInformation("Sending presentation pending notification for requestId: {RequestId}", requestId);

        var payload = new WebhookPayload
        {
            Event = "verification_pending",
            RequestId = requestId,
            CallerName = callerName,
            Message = $"Please scan the QR code to proceed with Verified ID verification.",
            QrCodeUrl = qrCode
        };

        SendNotification(new GenericWebhookNotification(payload, jsonSerializerOptions));
    }

    public void SendPresentationVerifiedNotification(Guid requestId, string callerName)
    {
        logger.LogInformation("Sending presentation verified notification for requestId: {RequestId}", requestId);

        var payload = new WebhookPayload
        {
            Event = "identity_verified",
            RequestId = requestId,
            CallerName = callerName,
            Message = "Verified ID presentation succeeded."
        };

        SendNotification(new GenericWebhookNotification(payload, jsonSerializerOptions));
    }

    public void SendCallbackCompletedNotification(Guid requestId, string callerName)
    {
        logger.LogInformation("Sending callback completed notification for requestId: {RequestId}", requestId);

        var payload = new WebhookPayload
        {
            Event = "callback_completed",
            RequestId = requestId,
            CallerName = callerName,
            Message = "Verification result delivered to caller."
        };

        SendNotification(new GenericWebhookNotification(payload, jsonSerializerOptions));
    }

    private async Task PostNotification(INotification notification)
    {
        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            configuration.NotificationWebhookUrl);

        httpRequest.Content = new StringContent(
            notification.ToJson(jsonSerializerOptions),
            new MediaTypeHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(httpRequest);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to send webhook notification. Status: {response.StatusCode}");
        }
    }
}

public record WebhookPayload
{
    public string Event { get; init; } = default!;
    public Guid RequestId { get; init; }
    public string CallerName { get; init; } = default!;
    public string Message { get; init; } = default!;
    public string? QrCodeUrl { get; init; }
}

public class GenericWebhookNotification(WebhookPayload payload, JsonSerializerOptions jsonSerializerOptions) : INotification
{
    public string Id { get; init; } = Guid.NewGuid().ToString();

    public string ToJson(JsonSerializerOptions _)
    {
        return JsonSerializer.Serialize(payload, jsonSerializerOptions);
    }
}
