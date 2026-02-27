using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using VIDVerifier.Models.Request;

namespace VIDVerifier;

/// <summary>
/// Stores per-request context: status, expiration, and the caller-provided
/// callback URL / display name so the callback function can reach the caller.
/// </summary>
public class RequestCache(IMemoryCache memoryCache, ILogger<RequestCache> logger)
{
    public void UpdateStatus(Guid requestId, string status, int expiration)
    {
        var timeToLiveInSeconds = GetRequestTimeToLive(expiration);
        memoryCache.Set(requestId, status, timeToLiveInSeconds);
        memoryCache.Set(GetRequestTimeToLiveKey(requestId), expiration, timeToLiveInSeconds);
        logger.LogInformation(
            $"Updated status of request '{requestId}' to '{status}' with time to live: {timeToLiveInSeconds.TotalSeconds}.");
    }

    /// <summary>
    /// Stores the caller context (callback URL, display name) alongside the request.
    /// </summary>
    public void SetCallerContext(Guid requestId, string callerCallbackUrl, string? callerName, int expiration)
    {
        var ttl = GetRequestTimeToLive(expiration);
        memoryCache.Set(GetCallerCallbackKey(requestId), callerCallbackUrl, ttl);
        if (callerName is not null)
        {
            memoryCache.Set(GetCallerNameKey(requestId), callerName, ttl);
        }
    }

    public bool TryGetCallerCallbackUrl(Guid requestId, [NotNullWhen(true)] out string? url)
    {
        if (!memoryCache.TryGetValue(GetCallerCallbackKey(requestId), out url) || url is null)
        {
            logger.LogWarning("Failed to get caller callback URL for request '{RequestId}'.", requestId);
        }
        return url is not null;
    }

    public string GetCallerName(Guid requestId)
    {
        if (memoryCache.TryGetValue<string>(GetCallerNameKey(requestId), out var name) && name is not null)
        {
            return name;
        }
        return "Verified ID verification";
    }

    public bool TryGetRequestStatus(Guid requestId, [NotNullWhen(true)] out string? status)
    {
        if (!memoryCache.TryGetValue(requestId, out status) || status is null)
        {
            logger.LogWarning($"Failed to get status for request '{requestId}'.");
        }

        return status is not null;
    }

    public bool TryGetRequestExpiration(Guid requestId, [NotNullWhen(true)] out int expiration)
    {
        if (!memoryCache.TryGetValue<string>(requestId, out var status)
            || status is null
            || !memoryCache.TryGetValue<int>(GetRequestTimeToLiveKey(requestId), out var expirationInt))
        {
            logger.LogWarning($"Failed to get expiration for request '{requestId}'.");
            expiration = default;
        }
        else
        {
            expiration = expirationInt;
        }

        return expiration != default;
    }

    private static string GetRequestTimeToLiveKey(Guid requestId) => $"{requestId}-exp";
    private static string GetCallerCallbackKey(Guid requestId) => $"{requestId}-callback";
    private static string GetCallerNameKey(Guid requestId) => $"{requestId}-callerName";

    private static TimeSpan GetRequestTimeToLive(int expiration)
    {
        var timeToLiveInSeconds = Math.Max(0, expiration - DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        timeToLiveInSeconds += 5 * 60;
        return TimeSpan.FromSeconds(Convert.ToDouble(timeToLiveInSeconds));
    }
}
