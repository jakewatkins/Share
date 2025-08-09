# Gmail Service

The `GmailService` class provides integration with Google's Gmail API to retrieve email messages.

## Overview

The Gmail service uses Google's Gmail API v1 and OAuth2 authentication to securely access Gmail accounts. It follows the same patterns as other email services in this library.

## Configuration

### Required Azure Key Vault Secrets

The following secrets must be configured in your Azure Key Vault:

- `GoogleClientId` - OAuth2 client ID obtained from Google Cloud Console
- `GoogleClientSecret` - OAuth2 client secret obtained from Google Cloud Console
- `GoogleCalendarId` - The Gmail email address to authenticate (shared with calendar service)

### Setting up Google API Credentials

1. Go to the [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select an existing one
3. Enable the Gmail API
4. Create OAuth2 credentials (Desktop application type)
5. Store the Client ID and Client Secret in Azure Key Vault

## Features

- **OAuth2 Authentication**: Uses GoogleWebAuthorizationBroker for secure authentication
- **Oldest First Retrieval**: Emails are retrieved in chronological order (oldest first)
- **Unread Preservation**: Emails remain marked as unread after retrieval
- **HTML Preference**: HTML email bodies are preferred over plain text
- **Attachment Metadata**: Only attachment metadata is retrieved (name, type, size)
- **Rate Limiting**: Built-in rate limiting to respect Gmail API quotas

## Authentication Flow

The Gmail service uses the following authentication pattern:

```csharp
var userCredential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
    new ClientSecrets
    {
        ClientId = clientId,
        ClientSecret = clientSecret
    },
    new[] { GmailService.Scope.GmailModify },
    emailAddress, // Using GoogleCalendarId as user identifier
    CancellationToken.None);
```

## Usage Example

```csharp
using EmailAgent.Core;
using EmailAgent.Entities;
using EmailAgent.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

// Setup configuration
var configuration = new ConfigurationBuilder()
    .AddJsonFile("settings.json")
    .Build();

// Create logger
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<GmailService>();

// Initialize configuration
var agentConfig = new AgentConfiguration(configuration);

// Create Gmail service
var gmailService = new GmailService(agentConfig, logger);

// Create request
var request = new GetEmailRequest
{
    NumberOfEmails = 10
};

// Get emails
var response = await gmailService.GetEmail(request);

if (response.Success)
{
    Console.WriteLine($"Retrieved {response.Emails.Count} emails");
    foreach (var email in response.Emails)
    {
        Console.WriteLine($"From: {email.From}");
        Console.WriteLine($"Subject: {email.Subject}");
        Console.WriteLine($"Attachments: {email.Attachments.Count}");
    }
}
else
{
    Console.WriteLine($"Error: {response.Message}");
}
```

## Error Handling

The Gmail service follows the library's error handling patterns:

- **Configuration Errors**: Throws `ArgumentException` for missing configuration values
- **Authentication Errors**: Throws `InvalidOperationException` for authentication failures
- **API Errors**: Returns error information in the `GetEmailResponse.Message` property

## API Permissions

The Gmail service requests the following OAuth2 scopes:

- `GmailService.Scope.GmailModify` - Allows reading and modifying Gmail messages
- Fallback to `GmailService.Scope.GmailReadonly` is acceptable

## Rate Limiting

The service includes built-in rate limiting with a minimum 100ms delay between API calls to respect Gmail API quotas and prevent throttling.

## Message Processing

### Email Headers
The service extracts standard email headers:
- From, To, CC, BCC addresses
- Subject
- Sent date/time

### Body Content
- HTML bodies are preferred over plain text
- Both multipart and single-part messages are supported
- Content is decoded from Gmail's base64url format

### Attachments
Only attachment metadata is retrieved:
- Filename
- MIME type
- Size in bytes
- No attachment content is downloaded

## Logging

The service provides comprehensive logging for debugging and monitoring:

- Authentication events
- API call metrics
- Email processing statistics
- Error conditions and exceptions

All logs use structured logging with relevant context information.
