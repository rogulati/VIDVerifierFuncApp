# VID Verifier Function App

An Azure Functions v4 application (**.NET 8, isolated worker**) that orchestrates [Microsoft Entra Verified ID](https://learn.microsoft.com/en-us/entra/verified-id/) presentation requests and delivers real-time verification status notifications to Microsoft Teams.

## Overview

VID Verifier acts as a backend service that sits between a calling application and the Microsoft Entra Verified ID service. It:

1. Accepts a verification request from a caller (e.g., a web app or automation).
2. Creates a Verified ID **presentation request** (optionally with FaceCheck liveness).
3. Returns a deep-link URL for the holder to scan.
4. Receives the **callback** from the Verified ID service when the holder presents credentials.
5. Sends structured **Teams notifications** at each stage (pending → verified → callback delivered).
6. POSTs the verification result back to the caller's webhook.

```
Caller ──POST──▶ /api/createPresentationRequest
                        │
                        ▼
              Entra Verified ID Service
                        │  callback
                        ▼
              /api/callback ──POST──▶ Caller Webhook
                        │
                        ▼
               Teams Notification
```

## Project Structure

```
VIDVerifierFuncApp/
├── VIDVerifier.sln
└── VIDVerifierFunctionApp/
    ├── Program.cs                          # Host builder & DI registration
    ├── Configuration.cs                    # Reads app settings / env vars
    ├── AccessTokenProvider.cs              # MSAL token acquisition via Key Vault secret
    ├── RequestCache.cs                     # In-memory cache for request state
    ├── CreatePresentationRequestFunction.cs# HTTP trigger – creates presentation requests
    ├── PresentationCallbackFunction.cs     # HTTP trigger – handles Verified ID callbacks
    ├── host.json                           # Azure Functions host configuration
    ├── VIDVerifier.csproj                  # Project file (.NET 8 / Functions v4)
    ├── Models/
    │   ├── Request/
    │   │   ├── StartFlowRequest.cs         # Inbound request from caller
    │   │   ├── CreatePresentationRequest.cs# Payload sent to Verified ID API
    │   │   ├── PresentationCallbackRequest.cs # Callback from Verified ID service
    │   │   ├── Callback.cs
    │   │   └── VerificationResultCallback.cs  # Payload POSTed back to caller
    │   └── Response/
    │       └── CreateRequestResponse.cs    # Response from Verified ID API
    ├── NotificationCenter/
    │   ├── INotificationCenter.cs          # Notification abstraction
    │   ├── TeamsNotificationCenter.cs      # Teams webhook implementation
    │   ├── TeamsNotification.cs
    │   ├── AttachmentBuilder.cs
    │   └── ...                             # Adaptive Card block types
    └── Properties/
        └── launchSettings.json
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
- An **Azure Key Vault** containing the client secret for the Entra app registration
- A **Microsoft Entra Verified ID** tenant with an app registration that has the `VerifiableCredentials.Create` permission
- A **Microsoft Teams Incoming Webhook** URL for notifications

## Configuration

The application reads configuration from environment variables (locally via `local.settings.json`). All values are **required** unless noted otherwise.

| Setting | Description |
|---|---|
| `keyVaultUrl` | Azure Key Vault URI (e.g., `https://my-vault.vault.azure.net/`) |
| `clientId` | App registration (client) ID for Verified ID API access |
| `clientSecretName` | Name of the secret in Key Vault holding the client secret |
| `clientApiResource` | Verified ID API resource/scope (e.g., `3db474b9-6a0c-4840-96ac-1fceb342124f/.default`) |
| `tenantId` | Microsoft Entra tenant ID |
| `origin` | Public base URL of this Function App (used to build the callback URL) |
| `teamsNotificationsEndpoint` | Teams Incoming Webhook URL |
| `defaultCredentialType` | *(optional)* Default credential type — defaults to `VerifiedEmployee` |
| `defaultAuthority` | *(optional)* Default DID authority — defaults to `did:web:eu-syntheticsdocumentprovider.azurewebsites.net` |

### Example `local.settings.json`

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "keyVaultUrl": "https://<your-vault>.vault.azure.net/",
    "clientId": "<app-registration-client-id>",
    "clientSecretName": "<key-vault-secret-name>",
    "clientApiResource": "3db474b9-6a0c-4840-96ac-1fceb342124f/.default",
    "tenantId": "<your-tenant-id>",
    "origin": "https://<your-function-app>.azurewebsites.net",
    "teamsNotificationsEndpoint": "https://outlook.office.com/webhook/..."
  }
}
```

## Deploy to Azure

Complete the [Prerequisites](#prerequisites) and [Configuration](#configuration) setup before deploying so that you have all the required parameter values.

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Frogulati%2FVIDVerifierFuncApp%2Fmaster%2FARMTemplate%2Ftemplate.json)

You will be prompted for the following parameters:

| Parameter | Description |
|---|---|
| **Function App Name** | Globally unique name for the Function App |
| **Key Vault URL** | Azure Key Vault URI containing the client secret |
| **Tenant ID** | Your Microsoft Entra tenant ID |
| **Client ID** | App registration client ID with Verified ID permissions |
| **Client Secret Name** | Name of the Key Vault secret holding the client credential |
| **Client API Resource** | Verified ID API scope (pre-filled with default) |
| **Teams Notifications Endpoint** | Incoming Webhook URL for your Teams channel |
| **Default Credential Type** | Credential type to request (defaults to `VerifiedEmployee`) |
| **Default Authority** | DID authority (defaults to `did:web:eu-syntheticsdocumentprovider.azurewebsites.net`) |

The template deploys:
- **Azure Function App** (Consumption plan, .NET 8 isolated worker, v4 runtime)
- **Storage Account** for the Functions host
- **Application Insights** for monitoring and logging
- **System-assigned Managed Identity** (grant it Key Vault access after deployment)

### Post-Deployment Steps

1. **Grant Key Vault access** — The Function App's managed identity needs `Get` permission on secrets. In the Key Vault's Access Policies (or RBAC), add the Function App's system-assigned identity with the *Key Vault Secrets User* role.
2. **Grant Verified ID API permission** — If using Managed Identity instead of client credentials, assign the `VerifiableCredential.Create.All` application role to the Function App's identity:
   ```powershell
   $TenantID = "<YOUR TENANT ID>"
   $AppName  = "<YOUR FUNCTION APP NAME>"
   $ApiAppId = "3db474b9-6a0c-4840-96ac-1fceb342124f"
   $PermissionName = "VerifiableCredential.Create.All"

   Connect-AzureAD -TenantId $TenantID
   $MSI = (Get-AzureADServicePrincipal -Filter "displayName eq '$AppName'")
   $ApiSP = Get-AzureADServicePrincipal -Filter "appId eq '$ApiAppId'"
   $Role = $ApiSP.AppRoles | Where-Object { $_.Value -eq $PermissionName -and $_.AllowedMemberTypes -contains "Application" }
   New-AzureAdServiceAppRoleAssignment -ObjectId $MSI.ObjectId -PrincipalId $MSI.ObjectId -ResourceId $ApiSP.ObjectId -Id $Role.Id
   ```

## Getting Started

### Build

```bash
cd VIDVerifierFunctionApp
dotnet build
```

### Run Locally

```bash
cd VIDVerifierFunctionApp
func start
```

The function app will start on port **7120** (as configured in `launchSettings.json`).

## API Reference

### `POST /api/createPresentationRequest`

Creates a new Verified ID presentation request.

**Request Body:**

```json
{
  "state": "<correlation-id>",
  "callerCallbackUrl": "https://caller.example.com/webhook",
  "callerName": "Role activation",
  "credentialType": "VerifiedEmployee",
  "requireFaceCheck": true
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `state` | `string` | **Yes** | Correlation ID passed through to the caller's callback |
| `callerCallbackUrl` | `string` | **Yes** | URL to POST verification results to upon completion |
| `callerName` | `string` | No | Display name shown in the presentation request and Teams cards |
| `credentialType` | `string` | No | Credential type to request (defaults to `VerifiedEmployee`) |
| `requireFaceCheck` | `bool` | No | Whether to require FaceCheck liveness (defaults to `true`) |

**Success Response (`200 OK`):**

```json
{
  "requestId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "url": "openid-vc://..."
}
```

### `POST /api/callback`

Receives callbacks from the Microsoft Entra Verified ID service. **This endpoint is not intended to be called directly by consumers** — its URL is automatically registered when creating a presentation request.

When the presentation status is `presentation_verified`, the function:
1. Sends a "verified" Teams notification.
2. POSTs a `VerificationResultCallback` to the caller's webhook:

```json
{
  "requestId": "<original-state>",
  "status": "verified",
  "message": "Verified ID presentation completed successfully.",
  "faceCheck": { ... }
}
```

## Architecture

| Component | Responsibility |
|---|---|
| **CreatePresentationRequestFunction** | HTTP trigger that builds and submits the presentation request to the Verified ID API |
| **PresentationCallbackFunction** | HTTP trigger that processes Verified ID callbacks and notifies the caller |
| **AccessTokenProvider** | Acquires OAuth tokens via MSAL using a client secret retrieved from Azure Key Vault |
| **RequestCache** | In-memory cache (`IMemoryCache`) that tracks request status, expiration, and caller context |
| **Configuration** | Reads all required settings from environment variables |
| **TeamsNotificationCenter** | Sends Adaptive Card notifications to a Teams channel via Incoming Webhook |

## Teams Notifications

The app sends three types of Teams notifications throughout the verification lifecycle:

1. **Verification Pending** — includes the QR code image for the holder to scan.
2. **Identity Verified** — confirms the presentation was successful.
3. **Callback Completed** — confirms the result was delivered to the caller's webhook.

## Observability

- **Application Insights** is integrated via the worker service SDK. Telemetry sampling is disabled for full fidelity.
- All major operations emit structured log messages with `RequestId` correlation.

## License

This project is for internal use. See your organization's licensing terms.
