
using Azure.Core;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;

namespace VIDVerifier.NotificationCenter;

public class TeamsNotificationCenter(
    Configuration configuration,
    IHttpClientFactory httpClientFactory,
    JsonSerializerOptions jsonSerializerOptions,
    ILogger<TeamsNotificationCenter> logger)
        : INotificationCenter
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();

    public void SendNotification(INotification notification)
    {
        logger.LogInformation($"Sending notification with ID '{notification.Id}'.");

        _ = PostNotification(notification).ContinueWith(t =>
        {
            if (t.IsCompletedSuccessfully)
            {
                logger.LogInformation($"Successfully sent notification '{notification.Id}'");
            }
            else
            {
                logger.LogInformation($"Failed to send notification '{notification.Id}'");
            }
        });
    }

    public void SendPresentationPendingNotification(Guid requestId, string qrCode, string callerName)
    {
        logger.LogInformation("Sending presentation pending notification for requestId: {RequestId}", requestId);

        var attachmentBuilder = new AttachmentBuilder()
            .WithTitle($"{callerName} — verification pending")
            .WithDescription($"[{requestId}] Please scan the QR code to proceed with Verified ID verification.")
            .AddImageBlock(qrCode, "QR Code");
        var notification = new TeamsNotification("message");
        notification.AddAttachment(attachmentBuilder);
        this.SendNotification(notification);
    }

    public void SendPresentationVerifiedNotification(Guid requestId, string callerName)
    {
        logger.LogInformation("Sending presentation verified notification for requestId: {RequestId}", requestId);

        var attachmentBuilder = new AttachmentBuilder()
            .WithTitle($"{callerName} — identity verified")
            .WithDescription($"[{requestId}] Verified ID presentation succeeded.");
        var notification = new TeamsNotification("message");
        notification.AddAttachment(attachmentBuilder);
        this.SendNotification(notification);
    }

    public void SendCallbackCompletedNotification(Guid requestId, string callerName)
    {
        logger.LogInformation("Sending callback completed notification for requestId: {RequestId}", requestId);

        var attachmentBuilder = new AttachmentBuilder()
            .WithTitle($"{callerName} — callback completed")
            .WithDescription($"[{requestId}] Verification result delivered to caller.");
        var notification = new TeamsNotification("message");
        notification.AddAttachment(attachmentBuilder);
        this.SendNotification(notification);
    }

    private async Task PostNotification(INotification notification)
    {
        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            configuration.TeamsNotificationsEndpoint);

        httpRequest.Content = new StringContent(
            notification.ToJson(jsonSerializerOptions),
            new MediaTypeHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(httpRequest);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Failed to send Notification.");
        }
    }
}
