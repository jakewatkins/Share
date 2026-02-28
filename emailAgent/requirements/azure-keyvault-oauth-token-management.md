# Azure Key Vault OAuth Token Management - Requirements & Implementation Plan

## 1. Product Overview

### 1.1 Document Title and Version

- PRD: Azure Key Vault OAuth Token Management for EmailAgent
- Version: 1.0

### 1.2 Product Summary

This project refactors the emailAgent application to use Azure Key Vault for secure OAuth bearer token storage instead of local file storage. The application will authenticate to Azure using system-assigned managed identity and store/retrieve OAuth tokens (Gmail, Outlook) in Azure Key Vault secrets. This enables the application to run unattended while maintaining secure access to email services without requiring interactive authentication after the initial setup.

The refactoring will introduce a new KeyVault service component that abstracts all Key Vault operations, allowing the existing email services (GmailService, OutlookService) to seamlessly transition from local file-based token storage to secure cloud-based storage.

## 2. Goals

### 2.1 Business Goals

- Enable unattended operation of emailAgent across different environments (local, Azure Functions, etc.)
- Improve security by moving OAuth tokens from local files to encrypted cloud storage
- Create reusable token management infrastructure for other applications
- Maintain existing functionality while improving security posture

### 2.2 User Goals

- Run emailAgent without manual authentication after initial setup
- Deploy emailAgent to Azure Functions or other cloud environments
- Share authentication tokens securely between multiple applications
- Reduce authentication friction while maintaining security

### 2.3 Non-Goals

- Changing existing email processing functionality
- Modifying email retrieval algorithms or business logic
- Adding new email service providers
- Changing the overall application architecture beyond token management

## 3. User Personas

### 3.1 Key User Types

- System administrators deploying emailAgent
- Developers building applications that need shared email access
- Operations teams managing automated email processing

### 3.2 Basic Persona Details

- **System Administrator**: Responsible for deploying and configuring emailAgent in various environments (local development, Azure Functions, containers)
- **Developer**: Creates applications that need to access email services using shared authentication
- **Operations Engineer**: Monitors and maintains automated email processing systems

### 3.3 Role-Based Access

- **Email Agent System Identity**: Read/write access to OAuth token secrets in Key Vault
- **Administrators**: Full access to Key Vault for secret management and troubleshooting
- **Developers**: Read-only access to Key Vault for debugging and development

## 4. Functional Requirements

- **Azure Managed Identity Authentication** (Priority: High)
  - Application must authenticate to Azure using system-assigned managed identity
  - No hard-coded credentials or certificates in application code
  - Seamless integration with existing DefaultAzureCredential usage

- **Key Vault Token Storage Service** (Priority: High)
  - New KeyVaultService class for abstracting all Key Vault operations
  - Dependency injection compatible with IConfiguration
  - Support for storing, retrieving, and updating OAuth tokens as Key Vault secrets

- **Gmail Token Migration** (Priority: High)
  - Refactor GmailService to use KeyVaultService instead of FileDataStore
  - Migrate existing local tokens to Key Vault during first run
  - Maintain existing token refresh functionality

- **Outlook Token Migration** (Priority: High)
  - Refactor OutlookService to use KeyVaultService instead of MSAL local cache
  - Store MSAL tokens in Key Vault as JSON strings
  - Maintain existing interactive authentication flow for initial setup

- **Configuration Management** (Priority: Medium)
  - Key Vault name specified in settings.json
  - No additional configuration required for managed identity
  - Clear separation between local development and cloud deployment configurations

## 5. User Experience

### 5.1 Entry Points & First-Time User Flow

- Administrator deploys application to Azure environment
- System-assigned managed identity is automatically created
- Administrator grants Key Vault access to the managed identity
- Application runs and prompts for initial authentication to email services
- Subsequent runs operate without user intervention

### 5.2 Core Experience

- **Initial Setup**: Administrator configures managed identity permissions once
  - This ensures secure, automated access to Key Vault
  - No ongoing credential management required

- **First Authentication**: User authenticates interactively to email services once
  - OAuth tokens are securely stored in Key Vault
  - Tokens are automatically refreshed and updated in Key Vault

- **Subsequent Runs**: Application operates without user intervention
  - Tokens retrieved from Key Vault automatically
  - Authentication happens transparently in background

### 5.3 Advanced Features & Edge Cases

- Token expiration handled automatically with refresh
- Failed Key Vault access falls back to interactive authentication
- Support for multiple email accounts with separate token storage
- Migration of existing local tokens during upgrade

### 5.4 UI/UX Highlights

- No UI changes to existing application
- Clear logging for authentication steps and token operations
- Detailed error messages for troubleshooting authentication issues

## 6. Narrative

The developer deploys emailAgent to an Azure environment and configures the system-assigned managed identity with Key Vault permissions. On the first run, the application prompts for authentication to Gmail and Outlook services, then securely stores the resulting OAuth tokens in Azure Key Vault. On subsequent runs, the application automatically retrieves these tokens from Key Vault and operates without user intervention, ensuring secure and reliable automated email processing.

## 7. Success Metrics

### 7.1 User-Centric Metrics

- Zero authentication prompts after initial setup
- Support for deployment in multiple environments (local, Azure Functions, containers)
- Successful migration from existing local token storage

### 7.2 Business Metrics

- Reduced security incidents related to exposed credentials
- Increased deployment flexibility across cloud environments
- Improved application reliability through automated token management

### 7.3 Technical Metrics

- 100% compatibility with existing email processing functionality
- Zero downtime during migration from local to Key Vault storage
- Successful token refresh operations without user intervention

## 8. Technical Considerations

### 8.1 Integration Points

- Azure Key Vault for secure secret storage
- System-assigned managed identity for authentication
- Existing GmailService and OutlookService classes
- Microsoft.Extensions.DependencyInjection for service registration
- Current AgentConfiguration class for settings management

### 8.2 Data Storage & Privacy

- OAuth tokens encrypted at rest in Azure Key Vault
- No sensitive data in application logs or configuration files
- Token access limited to authorized managed identities
- Compliance with Azure security best practices

### 8.3 Scalability & Performance

- Key Vault operations add minimal latency to token retrieval
- Token caching to reduce Key Vault API calls
- Support for multiple concurrent applications using same tokens
- Rate limiting considerations for Key Vault API

### 8.4 Potential Challenges

- Managing token refresh timing across multiple applications
- Handle Key Vault service outages gracefully
- Migration path from existing local token storage
- Debugging authentication issues in automated environments

## 9. Milestones & Sequencing

### 9.1 Project Estimate

- **Small**: 1-2 weeks development time

### 9.2 Team Size & Composition

- **1 developer**: Backend development, Azure integration, testing

### 9.3 Suggested Phases

- **Phase 1**: Create KeyVaultService and update dependency injection (2-3 days)
  - Implement new KeyVaultService class
  - Add dependency injection configuration
  - Add required NuGet packages

- **Phase 2**: Refactor GmailService for Key Vault integration (2-3 days)
  - Update token storage to use KeyVaultService
  - Implement token migration from local files
  - Test Gmail authentication flow

- **Phase 3**: Refactor OutlookService for Key Vault integration (2-3 days)
  - Update MSAL configuration to use KeyVaultService
  - Implement token storage as JSON in Key Vault
  - Test Outlook authentication flow

- **Phase 4**: Testing and deployment documentation (1-2 days)
  - Create managed identity setup documentation
  - Test complete application flow
  - Create deployment instructions

## 10. User Stories

### 10.1. System Identity Authentication Setup

- **ID**: AKV-001
- **Description**: As a system administrator, I need to configure the emailAgent to authenticate to Azure using a system-assigned managed identity so that no credentials need to be stored in the application.
- **Acceptance criteria**:
  - Application uses DefaultAzureCredential for Azure authentication
  - No client secrets or certificates stored in application configuration
  - Managed identity can be identified and granted Key Vault permissions
  - Clear documentation on finding and configuring the managed identity

### 10.2. Key Vault Service Implementation

- **ID**: AKV-002
- **Description**: As a developer, I need a KeyVaultService class that handles all Key Vault operations so that email services can store and retrieve OAuth tokens securely.
- **Acceptance criteria**:
  - KeyVaultService class accepts IConfiguration in constructor for DI compatibility
  - Service reads Key Vault name from settings.json configuration
  - Methods for storing, retrieving, and updating secrets in Key Vault
  - Proper error handling and logging for Key Vault operations
  - Required NuGet packages identified and added to project

### 10.3. Gmail Token Migration

- **ID**: AKV-003
- **Description**: As a user, I need GmailService to store OAuth tokens in Key Vault instead of local files so that tokens are secure and accessible from any environment.
- **Acceptance criteria**:
  - GmailService refactored to use KeyVaultService instead of FileDataStore
  - Existing local Gmail tokens automatically migrated to Key Vault on first run
  - Token refresh operations update Key Vault secrets
  - Local token files can be safely deleted after migration
  - Gmail authentication functionality remains unchanged from user perspective

### 10.4. Outlook Token Migration

- **ID**: AKV-004
- **Description**: As a user, I need OutlookService to store MSAL tokens in Key Vault instead of local cache so that tokens are secure and accessible from any environment.
- **Acceptance criteria**:
  - OutlookService refactored to use KeyVaultService instead of MSAL local cache
  - MSAL tokens serialized and stored as JSON strings in Key Vault
  - Token refresh operations update Key Vault secrets
  - Interactive authentication flow maintained for initial setup
  - Outlook authentication functionality remains unchanged from user perspective

### 10.5. Configuration Management

- **ID**: AKV-005
- **Description**: As an administrator, I need the application to read Key Vault configuration from settings.json so that I can deploy to different environments without code changes.
- **Acceptance criteria**:
  - Key Vault name read from settings.json keyvaultName property (already exists)
  - No additional configuration required for managed identity authentication
  - Support for both local development and cloud deployment scenarios
  - Clear error messages when Key Vault access fails

### 10.6. Token Migration and Backward Compatibility

- **ID**: AKV-006
- **Description**: As a user upgrading from local token storage, I need the application to automatically migrate my existing tokens to Key Vault so that I don't need to re-authenticate to all services.
- **Acceptance criteria**:
  - Application detects existing local token files during first run
  - Local tokens automatically migrated to Key Vault with appropriate secret names
  - Migration process logged clearly for troubleshooting
  - Application continues to work after migration without requiring re-authentication
  - Local token files can be safely deleted after successful migration

### 10.7. Unattended Operation

- **ID**: AKV-007
- **Description**: As a system administrator, I need emailAgent to run without user intervention after initial setup so that it can operate in automated environments like Azure Functions.
- **Acceptance criteria**:
  - No authentication prompts after initial token storage in Key Vault
  - Token refresh operations happen automatically in background
  - Application gracefully handles temporary Key Vault outages
  - Clear logging for all authentication and token operations
  - Support for running as scheduled task, Azure Function, or container

### 10.8. Multi-Application Token Sharing

- **ID**: AKV-008
- **Description**: As a developer, I need multiple applications to safely share OAuth tokens from Key Vault so that I can build distributed systems that access email services.
- **Acceptance criteria**:
  - Consistent secret naming convention for tokens across applications
  - Token refresh by one application updates Key Vault for all consumers
  - No conflicts when multiple applications access same tokens concurrently
  - Appropriate Key Vault access permissions for different application identities

### 10.9. Deployment Documentation

- **ID**: AKV-009
- **Description**: As a system administrator, I need clear instructions for setting up managed identity and Key Vault permissions so that I can deploy the application successfully.
- **Acceptance criteria**:
  - Step-by-step guide for enabling system-assigned managed identity
  - Instructions for granting Key Vault access permissions to managed identity
  - Documentation on finding managed identity principal ID
  - Example Azure CLI commands for permission setup
  - Troubleshooting guide for common authentication issues

### 10.10. Error Handling and Fallback

- **ID**: AKV-010
- **Description**: As a user, I need the application to handle Key Vault failures gracefully so that temporary outages don't prevent email processing.
- **Acceptance criteria**:
  - Clear error messages when Key Vault access fails
  - Retry logic with exponential backoff for transient failures
  - Fallback to interactive authentication when Key Vault is unavailable
  - Logging of all authentication attempts and failures
  - Application continues to function with reduced capability during Key Vault outages

---

## Implementation Notes

### Required NuGet Packages

The following packages need to be added to the emailServices project:

```xml
<PackageReference Include="Azure.Security.KeyVault.Secrets" Version="4.6.0" />
<PackageReference Include="Azure.Identity" Version="1.12.0" />
```

Note: These packages may already be included based on the existing AgentConfiguration implementation.

### Key Vault Secret Naming Convention

OAuth tokens will be stored using the following naming convention:
- Gmail tokens: `gmail-token-{sanitized-email-address}`
- Outlook tokens: `outlook-token-{sanitized-email-address}`

Where sanitized email address has '@' and '.' characters replaced with hyphens.

### Managed Identity Setup

When deploying to Azure, the system-assigned managed identity will be automatically created. Administrators will need to:
1. Find the managed identity's Principal ID in the Azure portal
2. Grant "Key Vault Secrets User" role to this identity on the Key Vault
3. Ensure the Key Vault access policy allows "Get" and "Set" operations for secrets

This document provides the complete requirements and implementation plan for migrating emailAgent to use Azure Key Vault for OAuth token management with managed identity authentication.
