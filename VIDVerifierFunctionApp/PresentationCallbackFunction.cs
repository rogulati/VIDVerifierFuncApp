using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;
using VIDVerifier.Models.Request;
using VIDVerifier.NotificationCenter;

namespace VIDVerifier;

public class PresentationCallbackFunction(
    RequestCache requestCache,
    ILogger<PresentationCallbackFunction> logger,
    IHttpClientFactory httpClientFactory,
    INotificationCenter notificationCenter,
    JsonSerializerOptions jsonSerializerOptions)
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();

    [Function("PresentationCallback")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "callback")] HttpRequest req)
    {
        PresentationCallbackRequest presentationCallbackRequest;

        try
        {
            presentationCallbackRequest = await JsonSerializer.DeserializeAsync<PresentationCallbackRequest>(req.Body, jsonSerializerOptions)
                ?? throw new ArgumentNullException(nameof(req));
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Invalid PresentationCallback object.");
            return new BadRequestObjectResult("Invalid PresentationCallback object.");
        }

        logger.LogInformation("Callback called with valid data. RequestId: {RequestId}, RequestStatus: {RequestStatus}", presentationCallbackRequest.RequestId, presentationCallbackRequest.RequestStatus);

        if (!requestCache.TryGetRequestExpiration(presentationCallbackRequest.RequestId, out var expiration))
        {
            logger.LogError("Failed to get expiration for request '{RequestId}'.", presentationCallbackRequest.RequestId);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }

        requestCache.UpdateStatus(presentationCallbackRequest.RequestId, presentationCallbackRequest.RequestStatus, expiration);

        if (presentationCallbackRequest.RequestStatus == "presentation_verified")
        {
            logger.LogInformation(
                "Presentation request '{RequestId}' verified successfully. Face check: {FaceCheck}",
                presentationCallbackRequest.RequestId,
                presentationCallbackRequest.FaceCheck);

            var callerName = requestCache.GetCallerName(presentationCallbackRequest.RequestId);
            notificationCenter.SendPresentationVerifiedNotification(presentationCallbackRequest.RequestId, callerName);
            PerformCallerSuccessCallback(presentationCallbackRequest);
        }

        return new OkResult();
    }

    private void PerformCallerSuccessCallback(PresentationCallbackRequest callback)
    {
        if (!requestCache.TryGetCallerCallbackUrl(callback.RequestId, out var callerCallbackUrl))
        {
            logger.LogError("No caller callback URL found for request '{RequestId}'. Skipping callback.", callback.RequestId);
            return;
        }

        VerificationResultCallback result = new()
        {
            RequestId = callback.State,
            Status = "verified",
            Message = "Verified ID presentation completed successfully.",
            FaceCheck = callback.FaceCheck,
        };

        logger.LogInformation("Performing caller callback for request '{RequestId}' to '{CallbackUrl}'.", result.RequestId, callerCallbackUrl);

        _ = SendCallerCallback(callerCallbackUrl, result).ContinueWith(t =>
        {
            if (t.IsCompletedSuccessfully)
            {
                logger.LogInformation("Successfully performed caller callback for request '{RequestId}'.", callback.RequestId);
                var callerName = requestCache.GetCallerName(callback.RequestId);
                notificationCenter.SendCallbackCompletedNotification(callback.RequestId, callerName);
            }
            else
            {
                logger.LogError(t.Exception, "Failed to perform caller callback for request '{RequestId}'.", callback.RequestId);
            }
        });
    }

    private async Task SendCallerCallback(string callbackUrl, VerificationResultCallback result)
    {
        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            callbackUrl);
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(result, jsonSerializerOptions),
            new MediaTypeHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(httpRequest);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Caller callback for request '{RequestId}' failed with status code {StatusCode}.", result.RequestId, response.StatusCode);
            throw new Exception($"Caller callback failed with status {response.StatusCode}.");
        }
    }
}