# Email Agent

A .NET 8.0/9.0 multi-provider email retrieval agent that supports Gmail and Microsoft Outlook accounts. The agent retrieves emails from configured accounts and exports them to JSON files for further processing.

## 🚀 Features

- **Multi-Provider Support**: Works with both Gmail and Microsoft Outlook/Hotmail accounts
- **Personal Account Support**: Full support for personal Microsoft accounts (@outlook.com, @hotmail.com, @msn.com, etc.)
- **Secure Authentication**:
  - OAuth 2.0 for Gmail using Google APIs
  - Modern MSAL.NET authentication for Microsoft Graph API
- **Azure Key Vault Integration**: Secure credential storage
- **Flexible Configuration**: JSON-based configuration with environment-specific overrides
- **Structured Logging**: Comprehensive logging with Serilog
- **JSON Export**: Clean JSON output for email data processing
- **Folder Support**: Retrieve emails from specific folders (Inbox, Sent, Drafts, etc.)
- **Attachment Metadata**: Capture attachment information without downloading content

## 📋 Prerequisites

- .NET 8.0 or .NET 9.0 SDK
- Azure subscription (for Key Vault)
- Google Cloud Platform account (for Gmail access)
- Microsoft Azure App Registration (for Outlook access)

## 🏗️ Project Structure

```
emailAgent/
├── README.md                           # This file
├── emailAgent.sln                      # Solution file
├── emailAgent/                         # Main console application
│   ├── EmailAgent.csproj
│   ├── Program.cs                      # Entry point
│   ├── Configuration.cs                # Configuration classes
│   ├── EmailAccountProcessor.cs        # Main processing logic
│   ├── settings.json                   # Configuration file
│   └── Entities/
│       └── EmailAccount.cs
├── emailServices/                      # Shared email services library
│   ├── emailServices.csproj
│   ├── Core/
│   │   └── AgentConfiguration.cs       # Core configuration
│   ├── Entities/                       # Email entities
│   │   ├── Email.cs
│   │   ├── EmailAttachment.cs
│   │   ├── EmailFolder.cs
│   │   └── EmailService.cs
│   ├── Services/                       # Email service implementations
│   │   ├── GmailService.cs
│   │   ├── OutlookService.cs           # Modern Graph v5 implementation
│   │   └── OwaService.cs
│   └── Examples/                       # Usage examples
└── emailServices.Tests/                # Unit tests
```

## ⚙️ Setup

### 1. Clone and Build

```bash
git clone <repository-url>
cd emailAgent
dotnet restore
dotnet build
```

### 2. Azure Key Vault Setup

Create an Azure Key Vault and add the following secrets:

**For Gmail accounts:**
- `gmail-client-id`: Your Google OAuth client ID
- `gmail-client-secret`: Your Google OAuth client secret

**For Outlook accounts:**
- `outlook-client-id`: Your Azure app registration client ID
- `outlook-secret`: Your Azure app registration client secret

### 3. Google Cloud Platform Setup

1. Create a project in [Google Cloud Console](https://console.cloud.google.com)
2. Enable the Gmail API
3. Create OAuth 2.0 credentials
4. Add your credentials to Azure Key Vault

### 4. Microsoft Azure App Registration

1. Register an application in [Azure Portal](https://portal.azure.com)
2. Configure API permissions:
   - `Mail.Read`
   - `Mail.ReadWrite`
3. Set redirect URI: `http://localhost`
4. Support personal accounts by using the `/consumers` endpoint
5. Add credentials to Azure Key Vault

### 5. Configuration

Update `settings.json`:

```json
{
  "keyvaultName": "your-keyvault-name",
  "tempFolder": "./temp",
  "emailAccounts": [
    {
      "type": "gmail",
      "mailbox": "your-email@gmail.com",
      "enabled": true
    },
    {
      "type": "outlook",
      "mailbox": "your-email@outlook.com",
      "enabled": true
    }
  ],
  "serilog": {
    "minimumLevel": "Information",
    "writeTo": [
      { "name": "Console" },
      { "name": "File", "args": { "path": "logs/log-.txt", "rollingInterval": "Day" } }
    ]
  }
}
```

## 🏃‍♂️ Running the Application

```bash
# Build the solution
dotnet build

# Run the email agent
dotnet run --project emailAgent

# Or run with specific configuration
dotnet run --project emailAgent --configuration Release
```

## 📤 Output

The agent creates JSON files in the configured temp folder with the naming pattern:
```
{sanitized-email-address}-{timestamp}.json
```

Each JSON file contains an array of email objects with:
- Email metadata (from, to, subject, date)
- Body content (HTML preferred over plain text)
- Attachment information (metadata only)
- Service-specific identifiers

## 🔧 Development

### Building Individual Projects

```bash
# Build the shared library
dotnet build emailServices/

# Build the main application
dotnet build emailAgent/

# Run tests
dotnet test emailServices.Tests/
```

### VS Code Configuration

The project includes comprehensive VS Code settings for enhanced IntelliSense:
- Full C# language support with inlay hints
- Roslyn analyzers enabled
- Auto-formatting and organize imports on save
- GitHub Copilot integration
- Comprehensive suggestion and completion support

## 🔒 Security Notes

- All sensitive credentials are stored in Azure Key Vault
- OAuth flows use secure redirect URIs
- Personal Microsoft accounts are supported via the `/consumers` endpoint
- No email content is logged (only metadata)
- Attachment content is not downloaded by default

## 🆕 Recent Updates

### Microsoft Graph v5 Migration (Latest)
- ✅ Upgraded from Microsoft Graph v4 to v5.95.0
- ✅ Removed deprecated `Microsoft.Graph.Auth` package
- ✅ Fixed personal Microsoft account authentication
- ✅ Custom MSAL authentication provider for Graph v5 compatibility
- ✅ Modern fluent API syntax implementation

## 📦 Dependencies

### Main Application
- Microsoft.Extensions.Configuration
- Microsoft.Extensions.Logging
- Serilog with file and console sinks
- Azure.Identity & Azure.Security.KeyVault.Secrets
- System.Text.Json

### Email Services Library
- **Microsoft Graph v5.95.0** (Outlook integration)
- Microsoft.Identity.Client (MSAL.NET authentication)
- Google.Apis.Gmail.v1 & Google.Apis.Auth (Gmail integration)
- Microsoft.Exchange.WebServices (OWA legacy support)

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add/update tests
5. Submit a pull request

## 📝 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🆘 Troubleshooting

### Authentication Issues

**Gmail**: Ensure your Google Cloud project has the Gmail API enabled and your OAuth consent screen is configured.

**Outlook Personal Accounts**: The application uses the `/consumers` endpoint specifically for personal Microsoft accounts. If you see "Use your work or school account instead", verify your app registration supports personal accounts.

**Azure Key Vault**: Ensure your Azure identity has proper access policies to read secrets from the Key Vault.

### Common Issues

- **Build errors**: Run `dotnet restore` to ensure all packages are installed
- **Authentication failures**: Check that all secrets are properly configured in Azure Key Vault
- **Permission denied**: Verify OAuth scopes and app registration permissions
- **File access**: Ensure the temp folder path exists and is writable

For more detailed troubleshooting, check the application logs in the `logs/` directory.
