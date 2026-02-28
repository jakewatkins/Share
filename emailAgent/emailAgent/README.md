# EmailAgent

EmailAgent is a .NET 9 console application that retrieves emails from multiple email accounts and prepares them for classification.

## Features

- Loads configuration from `settings.json`
- Supports multiple Gmail and Outlook accounts
- Uses Serilog for structured logging
- Integrates with the emailServices library
- Validates all required configuration settings

## Configuration

The application reads its configuration from `settings.json`. See the example configuration in the file.

## Dependencies

- References the emailServices library
- Uses Azure Key Vault for secrets management
- Requires .NET 9.0

## Usage

```bash
dotnet run
```

## Logging

The application logs to a daily rolling file using Serilog. Log files are created in the format `emailServices{date}.log`.

## Next Steps

This is the base application (Requirement 1) that provides:
- Configuration loading and validation
- Logging setup
- Basic application structure
- Email account enumeration

Future phases will add:
- Email retrieval functionality
- JSON file creation
- Email archiving
- LLM-based email classification