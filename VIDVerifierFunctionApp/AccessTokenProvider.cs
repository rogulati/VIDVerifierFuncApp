using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Identity.Client;

namespace VIDVerifier;   

public class AccessTokenProvider(Configuration configuration)
{
    private readonly SecretClient _secretClient = new(
        new Uri(configuration.KeyVaultUrl),
        new DefaultAzureCredential());

    private IConfidentialClientApplication? _singletonApp = null;

    public async Task<string> GetAccessToken()
    {
        if (_singletonApp is null)
        {
            await InitializeSingletonAppAsync();
        }

        var accessToken = await _singletonApp!
            .AcquireTokenForClient([configuration.ClientApiResource])
            .WithTenantId(configuration.TenantId)
            .ExecuteAsync();
        return accessToken.AccessToken;
    }

    private async Task InitializeSingletonAppAsync()
    {
        var clientSecret = await GetClientSecretAsync();

        _singletonApp = ConfidentialClientApplicationBuilder.Create(configuration.ClientId)
            .WithClientSecret(clientSecret)
        .Build();
    }

    private async Task<string> GetClientSecretAsync()
    {
        var secret = await _secretClient.GetSecretAsync(configuration.ClientSecretName);
        return secret.Value.Value;
    }
}
