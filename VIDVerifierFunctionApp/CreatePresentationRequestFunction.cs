using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using VIDVerifier.Models.Request;
using VIDVerifier.Models.Response;
using VIDVerifier.NotificationCenter;

namespace VIDVerifier;

public class CreatePresentationRequestFunction(
    ILogger<CreatePresentationRequestFunction> logger,
    AccessTokenProvider accessTokenProvider,
    Configuration configuration,
    IHttpClientFactory httpClientFactory,
    RequestCache requestCache,
    INotificationCenter notificationCenter,
    JsonSerializerOptions jsonSerializerOptions)
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();

    [Function("CreatePresentationRequest")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "createPresentationRequest")] HttpRequest req)
    {
        logger.LogInformation("CreatePresentationRequest function triggered.");
        StartFlowRequest request;

        try
        {
            request = await JsonSerializer.DeserializeAsync<StartFlowRequest>(req.Body, jsonSerializerOptions)
                ?? throw new ArgumentNullException(nameof(req));
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Invalid CreatePresentationRequest.");
            return new BadRequestObjectResult("Invalid CreatePresentationRequest.");
        }

        string accessToken;

        try 
        {
            accessToken = await accessTokenProvider.GetAccessToken();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to obtain access token.");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }

        var authority = configuration.DefaultAuthority;
        var credentialType = request.CredentialType ?? configuration.DefaultCredentialType;
        var requireFaceCheck = request.RequireFaceCheck ?? true;
        var callerName = request.CallerName ?? "Verified ID verification";

        logger.LogInformation("Creating presentation request with authority: {Authority}, credential: {CredentialType}, faceCheck: {FaceCheck}",
            authority, credentialType, requireFaceCheck);

        var credentialConfig = new Dictionary<string, object>
        {
            { "type", credentialType },
            { "purpose", $"Verified ID presentation for {callerName}" },
            { "schema",  new Dictionary<string, object>
                {
                    { "uri", credentialType },
                }
            },
        };

        if (requireFaceCheck)
        {
            credentialConfig["configuration"] = new Dictionary<string, object>
            {
                { "validation", new Dictionary<string, object>
                    {
                        { "faceCheck", new Dictionary<string, object>
                            {
                                { "sourcePhotoClaimName", "photo" },
                            }
                        },
                    }
                },
            };
        }

        CreatePresentationRequest createPresentationRequest = new()
        {
            Authority = authority,
            Registration = new Dictionary<string, object>
                {
                    { "clientName", callerName },
                    { "purpose", $"Verified ID presentation for {callerName}" },
                },
            Callback = new()
            {
                Url = $"{configuration.Origin}/api/callback",
                State = request.State,
            },
            IncludeQrCode = true,
            IncludeReceipt = false,
            RequestedCredentials = [ credentialConfig ],
        };

        logger.LogInformation($"Object created for presentation request with callback {createPresentationRequest.Callback.Url}");

        CreateRequestResponse createRequestResponse;

        try
        {
            createRequestResponse = await CreatePresentationRequestAsync(createPresentationRequest, accessToken);
            logger.LogInformation("Presentation request created successfully. RequestId: {RequestId}", createRequestResponse.RequestId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create presentation request.");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }

        requestCache.UpdateStatus(createRequestResponse.RequestId, "request_created", (int)createRequestResponse.Expiration!);
        requestCache.SetCallerContext(
            createRequestResponse.RequestId,
            request.CallerCallbackUrl,
            request.CallerName,
            (int)createRequestResponse.Expiration!);

        notificationCenter.SendPresentationPendingNotification(
            createRequestResponse.RequestId,
            createRequestResponse.QrCode!,
            callerName);

        createRequestResponse.Expiration = null;
        createRequestResponse.QrCode = null;

        logger.LogWarning(
            "CreatePresentationRequest completed successfully. RequestId: {RequestId} RequestUrl: {RequestUrl}",
            createRequestResponse.RequestId,
            createRequestResponse.Url);
        return new OkObjectResult(JsonSerializer.Serialize(createRequestResponse, jsonSerializerOptions));
    }

    private async Task<CreateRequestResponse> CreatePresentationRequestAsync(   
            CreatePresentationRequest createPresentationRequest,
            string accessToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://verifiedid.did.msidentity.com/v1.0/verifiableCredentials/createPresentationRequest");
        request.Headers.Authorization = new("Bearer", accessToken);
        request.Content = new StringContent(
            JsonSerializer.Serialize(createPresentationRequest, jsonSerializerOptions),
            new MediaTypeHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(request);
        await ValidateCreatePresentationResponse(response);

        var result = await response.Content.ReadFromJsonAsync<CreateRequestResponse>(jsonSerializerOptions)
            ?? throw new ArgumentNullException(nameof(response));

        if (result.QrCode is null)
        {
            logger.LogError("Received empty QR code for presentation request.");
            throw new ArgumentException(nameof(result.QrCode));
        }

        return result;
    }

    private async Task ValidateCreatePresentationResponse(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            try
            {
                logger.LogError($"Failed to create presentation request: {response.StatusCode}: '{await response.Content.ReadAsStringAsync()}'.");
            }
            catch (Exception)
            {
                logger.LogError($"Failed to create presentation request: {response.StatusCode}: 'Unknown reason'.");
            }

            throw new Exception("Failed to create presentation request.");
        }
    }
}