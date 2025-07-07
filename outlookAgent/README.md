# Outlook Agent

A .NET 8 library for accessing emails from Outlook.com using Microsoft Graph API.

## Features

- Retrieve emails from Outlook.com inbox
- Pagination support with configurable batch sizes
- Attachment handling with size limits
- Comprehensive logging with Serilog
- Token management with automatic refresh
- Base64 encoded attachment content

## Setup

### 1. Azure App Registration

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to "Azure Active Directory" > "App registrations"
3. Click "New registration"
4. Fill in the registration form:
   - Name: `Outlook Agent`
   - Supported account types: `Accounts in any organizational directory and personal Microsoft accounts`
   - Redirect URI: `Public client/native (mobile & desktop)` - `http://localhost`
5. After registration, note the **Application (client) ID**
6. Go to "API permissions" and add:
   - `Mail.Read` (Delegated)
   - `Mail.ReadWrite` (Delegated)
7. Click "Grant admin consent" if you have admin privileges

### 2. Environment Variables

Set the following environment variables:

```bash
export valetClientId="your-client-id-here"
export valetSecret="your-client-secret-here"  # Not used in current implementation, but required by code
```

### 3. Configuration

Update `settings.json` with your preferences:

```json
{
  "RetrievalCount": 50,
  "MaxAttachmentSize": 10485760,
  "Serilog": {
    // ... logging configuration
  }
}
```

## Usage

```csharp
using Microsoft.Extensions.Configuration;
using emailAgent;

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("settings.json", optional: false, reloadOnChange: true)
    .Build();

// Create the outlook agent
var agent = new outlookAgent(configuration);

// Get emails
var response = await agent.GetEmail(startIndex: 0, numberOfEmails: 50);

if (response.Success)
{
    Console.WriteLine($"Retrieved {response.Count} emails");
    foreach (var email in response.Emails)
    {
        Console.WriteLine($"{email.SentDateTime} | {email.From} | {email.Subject}");
    }
}
else
{
    Console.WriteLine($"Error: {response.Message}");
}
```

## Authentication Flow

1. **First run**: User will be prompted to sign in interactively
2. **Subsequent runs**: Token is automatically refreshed from `usertoken.json`
3. **Token storage**: Tokens are saved to `usertoken.json` in the working directory

## API Reference

### GetEmail Method

```csharp
Task<GetEmailResponse> GetEmail(int startIndex, int numberOfEmails)
```

**Parameters:**
- `startIndex`: Zero-based index of the first email to retrieve (0 = oldest email)
- `numberOfEmails`: Number of emails to retrieve

**Returns:** `GetEmailResponse` object with:
- `Success`: Boolean indicating if the operation was successful
- `Message`: Status message ("ok", "Last", or error details)
- `Count`: Number of emails actually retrieved
- `Emails`: Collection of email objects

### Email Object

```csharp
public class Email
{
    public string From { get; set; }
    public List<string> To { get; set; }
    public DateTime SentDateTime { get; set; }
    public string Subject { get; set; }
    public string MessageBody { get; set; }
    public List<EmailAttachment> Attachments { get; set; }
}
```

### EmailAttachment Object

```csharp
public class EmailAttachment
{
    public string Name { get; set; }
    public string Type { get; set; }
    public long Size { get; set; }
    public string Content { get; set; } // Base64 encoded content
}
```

## Configuration Options

| Setting | Description | Default |
|---------|-------------|---------|
| `RetrievalCount` | Number of emails to retrieve per batch | 50 |
| `MaxAttachmentSize` | Maximum attachment size in bytes | 10485760 (10MB) |
| `Serilog` | Logging configuration | See settings.json |

## Logging

The library uses Serilog for logging. Logs are written to:
- Console (for immediate feedback)
- File (`logs/outlookAgent-{date}.log`)

Log levels:
- `Information`: General operation info
- `Debug`: Detailed processing info
- `Warning`: Non-critical issues
- `Error`: Critical errors

## Error Handling

The library handles various error scenarios:
- Authentication failures
- Network connectivity issues
- Graph API errors
- Attachment processing errors

Errors are logged and returned in the `GetEmailResponse.Message` field.

## Limitations

- Only retrieves emails from the Inbox folder
- Simple index-based pagination (doesn't handle concurrent email changes)
- No retry logic for transient failures
- No cancellation support for long-running operations
