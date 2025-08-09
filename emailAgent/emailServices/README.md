# EmailAgent.emailServices

A .NET 8.0 shared library providing common entities and services for email processing across multiple email providers.

## Overview

This library provides shared entities and services for:
- Gmail service integration
- Microsoft Outlook service integration  
- Microsoft OWA (Outlook Web Access) integration

## Shared Entities

All entities are located in the `EmailAgent.Entities` namespace and are JSON serializable.

### Core Entities

#### `Email`
Represents an email message with the following properties:
- `Id` - Service-specific unique identifier
- `Service` - Which email service retrieved the email (Gmail, Outlook, OWA)
- `From` - Sender's email address
- `To` - Collection of recipient email addresses
- `CC` - Collection of CC recipient email addresses
- `BCC` - Collection of BCC recipient email addresses
- `SentDateTime` - When the email was sent
- `Subject` - Email subject
- `Body` - Email body content (HTML preferred over plain text)
- `Attachments` - Collection of email attachments

#### `EmailAttachment`
Represents an email attachment with:
- `Name` - Filename of the attachment
- `Type` - MIME type
- `Size` - Size in bytes
- `Content` - Base64 encoded content (null for oversized attachments)

#### `EmailService`
Enum identifying the email service:
- `Gmail`
- `Outlook` 
- `OWA`

### Request/Response Pattern

#### `GetEmailRequest`
Standard request object for email retrieval:
- `StartIndex` - Starting index for retrieval (0-based)
- `NumberOfEmails` - Number of emails to retrieve

#### `GetEmailResponse`
Standard response object for email operations:
- `Success` - Whether the operation succeeded
- `Message` - Status message or error details
- `Count` - Number of emails retrieved
- `Emails` - Collection of retrieved emails
- `Service` - Which service processed the request

## Configuration

The library uses `IConfiguration` for settings management and provides centralized configuration handling through the `AgentConfiguration` class.

### AgentConfiguration Class

The `AgentConfiguration` class (in `EmailAgent.Core` namespace) provides:
- Centralized loading of Azure Key Vault secrets
- Application configuration management
- Synchronous constructor that loads all secrets during initialization

```csharp
// Usage example
var configuration = new ConfigurationBuilder()
    .AddJsonFile("settings.json")
    .Build();

var agentConfig = new AgentConfiguration(configuration);
```

### Configuration File Structure

```json
{
  "keyvaultName": "your-azure-keyvault-name",
  "RetrievalCount": 500,
  "MaxAttachmentSize": 1048576,
  "Serilog": {
    // Serilog configuration
  }
}
```

### Configuration Properties

- `keyvaultName` - Azure Key Vault name for storing secrets
- `RetrievalCount` - Default number of emails to retrieve (default: 500)
- `MaxAttachmentSize` - Maximum attachment size in bytes (default: 1MB)
- `Serilog` - Logging configuration

## Email Services

All email services are in the `EmailAgent.Services` namespace and follow consistent patterns:

### GmailService

Provides Gmail integration using Google's Gmail API v1.

**Configuration Requirements:**
- `GoogleClientId` - OAuth2 client ID for Gmail API access
- `GoogleClientSecret` - OAuth2 client secret for Gmail API access  
- `GoogleCalendarId` - Email address for authentication (shared with calendar service)

**Features:**
- OAuth2 authentication using GoogleWebAuthorizationBroker
- Retrieves oldest emails first
- Preserves unread status
- HTML body preferred over plain text
- Attachment metadata only (filename, type, size)
- Rate limiting for API calls

**Usage:**
```csharp
var gmailService = new GmailService(agentConfiguration, logger);
var response = await gmailService.GetEmail(request);
```

### OutlookService

Provides Microsoft Outlook integration using Microsoft Graph API.

**Configuration Requirements:**
- `outlookClientId` - Azure AD application client ID
- `outlookSecret` - Azure AD application client secret

**Features:**
- Interactive OAuth2 authentication using PublicClientApplicationBuilder
- Microsoft Graph API integration
- Retrieves oldest emails first
- Preserves unread status
- HTML body preferred over plain text
- Attachment metadata only (filename, type, size)

**Usage:**
```csharp
var outlookService = new OutlookService(agentConfiguration, logger);
var response = await outlookService.GetEmail(request);
```

### OwaService

Provides OWA (Outlook Web Access) integration using Exchange Web Services.

**Configuration Requirements:**
- `owaServiceURI` - Exchange Web Services endpoint URL
- `owaPassword` - Password for authentication
- `owaEmailAddress` - Email address for authentication

**Features:**
- WebCredentials authentication
- Exchange Web Services (EWS) integration
- Retrieves oldest emails first
- Preserves unread status
- HTML body preferred over plain text
- Attachment metadata only (filename, type, size)

**Usage:**
```csharp
var owaService = new OwaService(agentConfiguration, logger);
var response = await owaService.GetEmail(request);
```

## Common Service Patterns

The library includes concrete service implementations in the `EmailAgent.Services` namespace.

### OwaService

Provides email retrieval functionality for Microsoft Exchange/OWA using Exchange Web Services.

```csharp
// Initialize service
var owaService = new OwaService(agentConfiguration, logger);

// Retrieve emails
var request = new GetEmailRequest { NumberOfEmails = 10 };
var response = await owaService.GetEmail(request);

if (response.Success)
{
    Console.WriteLine($"Retrieved {response.Count} emails");
    foreach (var email in response.Emails)
    {
        Console.WriteLine($"Subject: {email.Subject}");
    }
}
```

**Features:**
- WebCredentials authentication
- Oldest-first email retrieval
- Attachment metadata extraction
- HTML body preference
- Comprehensive error handling
- Integrated logging

**Configuration Requirements:**
- `OwaServiceURI` - Exchange server URL
- `OwaEmailAddress` - Authentication email
- `OwaPassword` - Authentication password

### OutlookService

Provides email retrieval functionality for Microsoft Outlook using Microsoft Graph API.

```csharp
// Initialize service
var outlookService = new OutlookService(agentConfiguration, logger);

// Retrieve emails
var request = new GetEmailRequest { StartIndex = 0, NumberOfEmails = 10 };
var response = await outlookService.GetEmail(request);

if (response.Success)
{
    Console.WriteLine($"Retrieved {response.Count} emails");
    foreach (var email in response.Emails)
    {
        Console.WriteLine($"Subject: {email.Subject}");
    }
}
```

**Features:**
- Interactive OAuth2 authentication
- Microsoft Graph API integration
- Oldest-first email retrieval
- Attachment metadata extraction
- HTML body preference
- Comprehensive error handling
- Integrated logging

**Configuration Requirements:**
- `OutlookClientId` - Azure AD application client ID
- `OutlookSecret` - Azure AD application client secret

## Design Principles

1. **Single Responsibility** - Each entity has a clear, focused purpose
2. **Service Agnostic** - Entities work across all email services
3. **JSON Serializable** - All entities can be serialized to/from JSON
4. **No Business Logic** - Entities are pure POCOs
5. **Consistent API** - Shared request/response pattern across services
6. **Avoid Duplication** - Single properties instead of service-specific variants

## Usage Example

```csharp
// Create a request
var request = new GetEmailRequest
{
    StartIndex = 0,
    NumberOfEmails = 10
};

// Process response
var response = new GetEmailResponse
{
    Success = true,
    Message = "ok",
    Count = 10,
    Service = EmailService.Gmail,
    Emails = emails
};

// Access email data
foreach (var email in response.Emails)
{
    Console.WriteLine($"From: {email.From}");
    Console.WriteLine($"Subject: {email.Subject}");
    Console.WriteLine($"Service: {email.Service}");
    Console.WriteLine($"Attachments: {email.Attachments.Count}");
}
```

## Dependencies

- .NET 8.0
- Microsoft.Extensions.Configuration
- Microsoft.Extensions.Logging.Abstractions
- Microsoft.Extensions.Logging
- Azure.Security.KeyVault.Secrets
- Azure.Identity
- Microsoft.Exchange.WebServices
- Microsoft.Graph
- Microsoft.Graph.Auth
- Microsoft.Identity.Client
- Google.Apis.Gmail.v1
- Google.Apis.Auth
- Serilog
- System.Text.Json
