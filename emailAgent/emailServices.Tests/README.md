# Email Services Unit Tests

This project contains unit tests for the emailServices library.

## Setup

1. Copy your Outlook credentials from another project's `settings.json` file or configure them directly:
   - `ClientId`: Your Azure AD application client ID
   - `TenantId`: Your Azure AD tenant ID
   - `RedirectUri`: OAuth redirect URI (typically `http://localhost`)

2. Update `settings.json` with your configuration

## Running Tests

Run all tests:
```bash
dotnet test
```

Run specific test:
```bash
dotnet test --filter "FullyQualifiedName~OutlookServiceTests.GetEmail_FromRecruitersFolder_ShouldReturnEmails"
```

Run with verbose output:
```bash
dotnet test --logger "console;verbosity=detailed"
```

## Test Coverage

### OutlookServiceTests
- `GetEmail_FromRecruitersFolder_ShouldReturnEmails`: Validates fetching emails from custom "Recruiters" folder
- `GetEmail_FromRecruitersFolder_WithLimit_ShouldRespectLimit`: Validates email count limit
- `GetEmail_FromRecruitersFolder_ShouldHaveRequiredProperties`: Validates email properties are populated

## Notes

- These are integration tests that require valid Outlook credentials
- Tests interact with real Outlook/Microsoft Graph API
- Ensure you have emails in the "Recruiters" folder before running tests
