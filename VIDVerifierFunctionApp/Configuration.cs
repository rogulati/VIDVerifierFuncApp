namespace VIDVerifier;

public class Configuration
{
    private const string KeyVaultUrlKey = "keyVaultUrl";
    private const string ClientIdKey = "clientId";
    private const string ClientSecretNameKey = "clientSecretName";
    private const string ClientApiResourceKey = "clientApiResource";
    private const string TenantIdKey = "tenantId";
    private const string OriginKey = "origin";
    private const string TeamsNotificationsEndpointKey = "teamsNotificationsEndpoint";
    private const string DefaultCredentialTypeKey = "defaultCredentialType";
    private const string DefaultAuthorityKey = "defaultAuthority";

    public string KeyVaultUrl { get; } = Environment.GetEnvironmentVariable(KeyVaultUrlKey)
        ?? throw new ArgumentNullException(nameof(KeyVaultUrlKey));

    public string ClientId { get; } = Environment.GetEnvironmentVariable(ClientIdKey)
        ?? throw new ArgumentNullException(nameof(ClientIdKey));

    public string ClientSecretName { get; } = Environment.GetEnvironmentVariable(ClientSecretNameKey)
        ?? throw new ArgumentNullException(nameof(ClientSecretNameKey));

    public string ClientApiResource { get; } = Environment.GetEnvironmentVariable(ClientApiResourceKey)
        ?? throw new ArgumentNullException(nameof(ClientApiResourceKey));

    public string TenantId { get; } = Environment.GetEnvironmentVariable(TenantIdKey)
        ?? throw new ArgumentNullException(nameof(TenantIdKey));

    public string Origin { get; } = Environment.GetEnvironmentVariable(OriginKey)
        ?? throw new ArgumentNullException(nameof(OriginKey));

    public string TeamsNotificationsEndpoint { get; } = Environment.GetEnvironmentVariable(TeamsNotificationsEndpointKey)
        ?? throw new ArgumentNullException(nameof(TeamsNotificationsEndpointKey));

    public string DefaultCredentialType { get; } = Environment.GetEnvironmentVariable(DefaultCredentialTypeKey)
        ?? "VerifiedEmployee";

    public string DefaultAuthority { get; } = Environment.GetEnvironmentVariable(DefaultAuthorityKey)
        ?? "did:web:eu-syntheticsdocumentprovider.azurewebsites.net";
}
