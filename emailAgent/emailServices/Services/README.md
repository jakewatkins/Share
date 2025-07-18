# Email Services

This folder contains the email service implementations for different email providers.

## Available Services

### OwaService

The `OwaService` class provides email retrieval functionality for Microsoft Exchange/OWA using Exchange Web Services (EWS).

#### Features

- **Exchange Web Services Integration**: Uses Microsoft.Exchange.WebServices library
- **WebCredentials Authentication**: Supports username/password authentication
- **Unified Email Model**: Maps EWS emails to shared `Email` entities
- **Attachment Metadata**: Retrieves attachment information (name, type, size) without content
- **Configurable Retrieval**: Supports custom email count and oldest-first ordering
- **Comprehensive Logging**: Uses ILogger for diagnostic information
- **Error Handling**: Proper exception handling with detailed error messages

#### Usage

```csharp
// Initialize with configuration and logger
var owaService = new OwaService(agentConfiguration, logger);

// Create request
var request = new GetEmailRequest
{
    NumberOfEmails = 10
};

// Retrieve emails
var response = await owaService.GetEmail(request);

// Process results
if (response.Success)
{
    Console.WriteLine($"Retrieved {response.Count} emails");
    foreach (var email in response.Emails)
    {
        Console.WriteLine($"Subject: {email.Subject}");
        Console.WriteLine($"From: {email.From}");
        Console.WriteLine($"Attachments: {email.Attachments.Count}");
    }
}
else
{
    Console.WriteLine($"Error: {response.Message}");
}
```

#### Configuration Requirements

The OwaService requires the following configuration values from `AgentConfiguration`:

- **OwaServiceURI**: The Exchange server URL
- **OwaEmailAddress**: The email address for authentication
- **OwaPassword**: The password for authentication

#### Email Mapping

The service maps Exchange EmailMessage objects to the unified `Email` entity:

- **Id**: Uses EWS UniqueId or generates a GUID
- **Service**: Always set to `EmailService.OWA`
- **Subject**: Email subject line
- **From**: Sender's email address
- **To/CC/BCC**: Recipient lists converted to string lists
- **SentDateTime**: Email received date/time
- **Body**: Email body (HTML preferred over plain text)
- **Attachments**: File attachment metadata only

#### Behavior

- **Retrieval Order**: Oldest emails first (ascending by DateTimeReceived)
- **Email Status**: Leaves emails marked as unread
- **Attachment Handling**: Retrieves metadata only (name, type, size)
- **Body Format**: Prefers HTML over plain text when multiple formats available
- **Error Handling**: Returns errors in response object, throws exceptions for configuration issues

#### Dependencies

- Microsoft.Exchange.WebServices (2.2.0)
- Microsoft.Extensions.Logging.Abstractions
- EmailAgent.Core (AgentConfiguration)
- EmailAgent.Entities (Email models)

#### Notes

- The Exchange Web Services library is a legacy .NET Framework package, which causes a compatibility warning in .NET 8
- Authentication failures will result in specific error messages for easier debugging
- The service awaits all EWS operations to provide a synchronous interface to clients
