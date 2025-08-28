# Test Email Services

A console application that demonstrates the use of the `emailServices` library by testing all three email service implementations: OwaService, GmailService, and OutlookService.

## Overview

This test application serves as a practical example of how to use the emailServices library. It initializes each email service and retrieves a few emails from each provider to verify functionality.

## Configuration

### settings.json

The application requires a `settings.json` file with the following structure:

```json
{
  "keyvaultName": "your-azure-keyvault-name",
  "RetrievalCount": 10,
  "MaxAttachmentSize": 1048576,
  "Serilog": {
    "Using": [ "Serilog.Sinks.File" ],
    "MinimumLevel": {
      "Default": "Information"
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "testEmailServices.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  }
}
```

### Azure Key Vault Secrets

Before running the test application, ensure the following secrets are configured in your Azure Key Vault:

**OWA Service:**
- `owaServiceURI` - Exchange Web Services endpoint URL
- `owaPassword` - Password for authentication
- `owaEmailAddress` - Email address for authentication

**Gmail Service:**
- `GoogleClientId` - OAuth2 client ID from Google Cloud Console
- `GoogleClientSecret` - OAuth2 client secret from Google Cloud Console
- `GoogleCalendarId` - Gmail email address for authentication

**Outlook Service:**
- `outlookClientId` - Azure AD application client ID
- `outlookSecret` - Azure AD application client secret

## Running the Application

1. **Build the application:**
   ```bash
   dotnet build
   ```

2. **Run the application:**
   ```bash
   dotnet run
   ```

3. **Expected Output:**
   The application will test each email service and display:
   - Progress messages as it works through each service
   - For each email retrieved: `SentDateTime - EmailService - From - Subject`
   - Success or error messages for each service

## Sample Output

```
=== Starting Email Services Test ===

Loading configuration...
Configuration loaded successfully

Testing OWA Service...
OWA Service: Retrieved 5 emails
2025-08-10 09:30:15 - OWA - john.doe@company.com - Meeting Tomorrow
2025-08-10 08:45:22 - OWA - jane.smith@company.com - Project Update
...

Testing Gmail Service...
Gmail Service: Retrieved 5 emails
2025-08-10 10:15:33 - Gmail - support@google.com - Account Security Alert
2025-08-10 09:22:11 - Gmail - newsletter@example.com - Weekly Newsletter
...

Testing Outlook Service...
Outlook Service: Retrieved 5 emails
2025-08-10 11:05:44 - Outlook - team@microsoft.com - Office 365 Updates
2025-08-10 10:30:55 - Outlook - admin@company.com - Policy Changes
...

=== Email Services Test Completed ===
```

## Error Handling

The application includes comprehensive error handling:

- **Configuration Errors:** Missing Azure Key Vault configuration
- **Authentication Errors:** Invalid credentials for any email service
- **Service Errors:** Network issues, API rate limits, etc.

All errors are logged to both console and the log file (`testEmailServices.log`).

## Project Structure

```
testEmailServices/
├── Program.cs              # Application entry point with configuration setup
├── TestEmailServices.cs    # Main test class that exercises all email services
├── settings.json          # Configuration file
├── testEmailServices.csproj # Project file with dependencies
└── README.md              # This documentation
```

## Dependencies

- emailServices (Project Reference)
- Microsoft.Extensions.Configuration
- Microsoft.Extensions.Logging
- Serilog for structured logging

## Notes

- Each service is tested independently - if one fails, the others will still be tested
- The application retrieves only 5 emails per service for testing purposes
- All logging is configured through Serilog with file output
- The application uses the same configuration patterns as the emailServices library
