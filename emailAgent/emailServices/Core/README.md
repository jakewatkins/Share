# EmailAgent.Core - Configuration Loader

This module provides the `AgentConfiguration` class that handles loading configuration settings from both Azure Key Vault and application configuration files.

## AgentConfiguration Class

The `AgentConfiguration` class is responsible for:
- Loading general settings from `settings.json` 
- Retrieving all secrets from Azure Key Vault during construction
- Providing a single configuration interface for email services

### Usage

```csharp
using Microsoft.Extensions.Configuration;
using EmailAgent.Core;

// Build configuration from settings.json
var configuration = new ConfigurationBuilder()
    .AddJsonFile("settings.json")
    .Build();

// Create AgentConfiguration - loads all Key Vault secrets during construction
var agentConfig = new AgentConfiguration(configuration);

// Access Key Vault secrets
var owaUri = agentConfig.OwaServiceURI;
var googleClientId = agentConfig.GoogleClientId;
var outlookSecret = agentConfig.OutlookSecret;

// Access general configuration
var retrievalCount = agentConfig.RetrievalCount;
var maxAttachmentSize = agentConfig.MaxAttachmentSize;
```

### Configuration Requirements

#### settings.json
```json
{
  "keyvaultName": "your-key-vault-name",     // Required
  "RetrievalCount": 500,                     // Optional, defaults to 500
  "MaxAttachmentSize": 1048576              // Optional, defaults to 1MB
}
```

#### Azure Key Vault Secrets
The following secrets must exist in your Key Vault:
- `owaServiceURI` - OWA service endpoint
- `owaPassword` - OWA authentication password
- `owaEmailAddress` - OWA email address
- `googleCalendarId` - Google Calendar ID
- `googleClientId` - Google OAuth client ID
- `googleClientSecret` - Google OAuth client secret
- `outlookClientId` - Outlook OAuth client ID
- `outlookSecret` - Outlook OAuth secret

### Error Handling

- **ArgumentException**: Thrown when `keyvaultName` is missing from configuration
- **InvalidOperationException**: Thrown when unable to retrieve secrets from Key Vault

### Authentication

The class uses `DefaultAzureCredential` for Key Vault authentication. Ensure your application has proper Azure authentication configured (managed identity, service principal, or development credentials).

### Implementation Details

- Constructor is synchronous - uses `.GetAwaiter().GetResult()` to wait for async Key Vault operations
- All Key Vault secrets are loaded eagerly during construction
- Configuration values are validated (positive integers for counts/sizes)
- Missing optional configuration values use sensible defaults
