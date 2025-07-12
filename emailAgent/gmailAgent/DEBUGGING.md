# Gmail Agent - Debugging Guide

## Prerequisites for Debugging

1. **Azure Setup:**
   - Run `az login` to authenticate with Azure
   - Ensure you have access to your Azure Key Vault
   - Update `keyvaultName` in `settings.json` or `settings.development.json`

2. **Google API Setup:**
   - Enable Gmail API in Google Cloud Console
   - Create OAuth2 credentials (Client ID and Secret)
   - Store credentials in Azure Key Vault as:
     - `googleClientId`
     - `googleClientSecret`
     - `googleCalendarId` (the email address/user to access)

## Debugging in Visual Studio Code

### Method 1: Using the Debugger
1. Open the project in VS Code
2. Set breakpoints in the code where you want to debug
3. Press `F5` or use `Run > Start Debugging`
4. Select "Debug Gmail Agent" configuration
5. The application will build and start with the debugger attached

### Method 2: Using Terminal
```bash
# Build the project
dotnet build

# Run the project
dotnet run

# Run with specific configuration
DOTNET_ENVIRONMENT=Development dotnet run
```

## Configuration Files

- **`settings.json`**: Production configuration
- **`settings.development.json`**: Development configuration (optional)
  - Lower RetrievalCount for faster testing
  - More verbose logging
  - Console logging enabled

## Debugging Tips

1. **Set Breakpoints:**
   - `gmailAgent.cs` line ~45: Constructor initialization
   - `gmailAgent.cs` line ~60: GetEmail method entry
   - `gmailAgent.cs` line ~120: Gmail service initialization
   - `gmailAgent.cs` line ~180: Message conversion

2. **Watch Variables:**
   - `_configuration` - Check if settings are loaded correctly
   - `result.Success` - Check API call success
   - `result.Message` - Check error messages
   - `messages.Count` - Check how many messages were retrieved

3. **Common Issues:**
   - Azure authentication failures
   - Key Vault access issues
   - Gmail API not enabled
   - OAuth consent not completed
   - Rate limiting (should see delays)

## Log Files

Logs are written to:
- Console (in development)
- `logs/gmailAgent-YYYY-MM-DD.log` files

Check logs for detailed error information and API call traces.

## Environment Variables

You can set these for different debugging scenarios:
- `DOTNET_ENVIRONMENT=Development` - Uses development settings
- `AZURE_CLIENT_ID` - Override Azure authentication
- `AZURE_TENANT_ID` - Override Azure tenant

## Testing Without Gmail API

For initial testing, you can comment out the Gmail API calls and use mock data to test the configuration and logging setup.
