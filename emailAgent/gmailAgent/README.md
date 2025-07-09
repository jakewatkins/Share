# Gmail Agent Library

A C# library for accessing Gmail emails using the Gmail API with Azure Key Vault integration.

## Features

- Retrieves emails from Gmail inbox using Google's Gmail API
- Supports OAuth authentication with credentials stored in Azure Key Vault
- Handles email attachments with configurable size limits
- Provides both HTML and plain text message bodies
- Includes comprehensive logging with Serilog
- Implements rate limiting to avoid API limits

## Setup

### Prerequisites

1. Azure Key Vault with the following secrets:
   - `googleClientId`: Google OAuth2 Client ID
   - `googleClientSecret`: Google OAuth2 Client Secret
   - `googleCalendarId`: The mailbox/user ID to access

2. Authenticated with Azure (e.g., `az login`)

3. Gmail API enabled in Google Cloud Console

### Configuration

Update `settings.json` with your configuration:

```json
{
  "keyvaultName": "your-keyvault-name",
  "RetrievalCount": 500,
  "MaxAttachmentSize": 1048576,
  "Serilog": {
    // Serilog configuration
  }
}
```

## Usage

```csharp
using emailAgent;
using Microsoft.Extensions.Configuration;

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("settings.json")
    .Build();

// Create Gmail agent
var agent = new gmailAgent(configuration);

// Retrieve emails
var result = await agent.GetEmail(startIndex: 0, numberOfEmails: 10);

if (result.Success)
{
    foreach (var email in result.Emails)
    {
        Console.WriteLine($"From: {email.From}");
        Console.WriteLine($"Subject: {email.Subject}");
        Console.WriteLine($"Date: {email.SentDateTime}");
        Console.WriteLine($"Attachments: {email.Attachments.Count}");
    }
}
```

## API Reference

### gmailAgent Class

#### Constructor
- `gmailAgent(IConfiguration configuration)`: Creates a new instance with the provided configuration

#### Methods
- `Task<GetEmailResult> GetEmail(int startIndex, int numberOfEmails)`: Retrieves emails from the inbox

### Email Class

Properties:
- `EmailMessageID`: Google Message ID
- `From`: Sender email address
- `To`: List of recipient email addresses
- `CC`: List of CC recipient email addresses
- `BCC`: List of BCC recipient email addresses
- `SentDateTime`: Date and time the email was sent
- `Subject`: Email subject
- `HtmlMessageBody`: HTML version of the message body
- `PlainMessageBody`: Plain text version of the message body
- `Attachments`: List of email attachments

### EmailAttachment Class

Properties:
- `Name`: Attachment filename
- `Type`: MIME type
- `Size`: Size in bytes
- `Content`: Base64 encoded content (null for large attachments)

### GetEmailResult Class

Properties:
- `Success`: Boolean indicating if the operation was successful
- `Message`: Status message or error details
- `Count`: Number of emails retrieved
- `Emails`: List of retrieved emails

## Configuration Settings

- `keyvaultName`: Name of the Azure Key Vault containing secrets
- `RetrievalCount`: Default number of emails to retrieve (default: 500)
- `MaxAttachmentSize`: Maximum attachment size in bytes (default: 1MB)
- `Serilog`: Serilog configuration for logging

## Error Handling

The library handles various error conditions:
- Azure Key Vault access errors
- Gmail API authentication errors
- Network connectivity issues
- Rate limiting

All errors are logged and returned in the `GetEmailResult.Message` property.

## Rate Limiting

The library implements a simple rate limiting mechanism to avoid exceeding Gmail API limits:
- Maximum 1 API call per second
- Automatic delays between API calls

## Logging

The library uses Serilog for structured logging. Configure logging in `settings.json` to control log levels and output destinations.
