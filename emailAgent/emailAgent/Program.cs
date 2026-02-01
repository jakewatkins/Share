using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using EmailAgent.Core;
using EmailAgent.Entities;
using System.Text.Json;

namespace EmailAgent;

class Program
{
    private static IConfiguration? _configuration;
    private static ILogger<Program>? _logger;
    private static EmailAgentConfiguration? _appConfig;

    static async Task Main(string[] args)
    {
        try
        {
            // Setup configuration
            SetupConfiguration();

            // Setup logging
            SetupLogging();

            // Load application configuration
            LoadApplicationConfiguration();

            _logger!.LogInformation("Email Agent starting up...");
            _logger.LogInformation("Loaded configuration for {AccountCount} email accounts", _appConfig!.EmailAccounts.Count);

            // Create agent configuration with better error handling
            AgentConfiguration agentConfiguration = new AgentConfiguration(_configuration);
            var emailAccountProcessor = new EmailAccountProcessor(agentConfiguration, GetLogger<EmailAccountProcessor>());

            // Log the email accounts (without sensitive data)
            foreach (var account in _appConfig.EmailAccounts)
            {
                if (true == account.Enabled)
                {
                    _logger.LogInformation("Account configured: {Type} - {Mailbox}", account.Type, account.Mailbox);
                    var emails = await emailAccountProcessor.GetEmails(account);
                    if (null != emails)
                    {
                        SaveEmailAsJson(account, emails);
                    }
                }
                else
                {
                    _logger.LogInformation($"Skking {account.Mailbox} - it has been disabled");
                }
            }

            _logger.LogInformation("Email Agent completed successfully");
        }
        catch (Exception ex)
        {
            if (_logger != null)
            {
                _logger.LogError(ex, "Fatal error occurred in Email Agent");
            }
            else
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
            }
            Environment.Exit(1);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    /// <summary>
    /// Sets up the configuration from settings.json
    /// </summary>
    private static void SetupConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("settings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("settings.development.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        _configuration = builder.Build();
    }

    /// <summary>
    /// Sets up Serilog logging based on configuration
    /// </summary>
    private static void SetupLogging()
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(_configuration!)
            .CreateLogger();

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddSerilog();
        });

        _logger = loggerFactory.CreateLogger<Program>();
    }

    /// <summary>
    /// Creates a logger factory for dependency injection
    /// </summary>
    private static ILoggerFactory CreateLoggerFactory()
    {
        return LoggerFactory.Create(builder =>
        {
            builder.AddSerilog();
        });
    }

    /// <summary>
    /// Get a type specific logger
    /// </summary>
    public static ILogger<T> GetLogger<T>()
    {
        var loggerFactory = CreateLoggerFactory();
        return loggerFactory.CreateLogger<T>();
    }

    /// <summary>
    /// Loads the application-specific configuration
    /// </summary>
    private static void LoadApplicationConfiguration()
    {
        _appConfig = new EmailAgentConfiguration();
        _configuration!.Bind(_appConfig);

        // Validate required configuration
        if (string.IsNullOrWhiteSpace(_appConfig.KeyvaultName))
            throw new InvalidOperationException("keyvaultName is required in configuration");

        if (string.IsNullOrWhiteSpace(_appConfig.TempFolder))
            throw new InvalidOperationException("TempFolder is required in configuration");

        if (_appConfig.EmailAccounts.Count == 0)
            throw new InvalidOperationException("At least one email account must be configured");

        // Validate each email account
        foreach (var account in _appConfig.EmailAccounts)
        {
            if (string.IsNullOrWhiteSpace(account.Type))
                throw new InvalidOperationException("Email account type is required");

            if (string.IsNullOrWhiteSpace(account.Mailbox))
                throw new InvalidOperationException("Email account mailbox is required");

            if (account.Type.ToLower() != "gmail" && account.Type.ToLower() != "outlook")
                throw new InvalidOperationException($"Unsupported email account type: {account.Type}");
        }

        _logger!.LogInformation("Configuration validation completed successfully");
    }

    private static string GetMailBoxName(string emailAddress)
    {
        return emailAddress.Replace("@", "").Replace(".", "");
    }
    private static void SaveEmailAsJson(EmailAccount account, List<Email> emails)
    {
        try
        {
            // Setup file name - TempFolder, the account name with the '@' and top level domain (ie '.com') removed, and the date time as "yyyyMMddHHmmss" with a json extension
            var sanitizedMailbox = GetMailBoxName(account.Mailbox);

            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var fileName = $"{sanitizedMailbox}-{timestamp}.json";
            var filePath = Path.Combine(_appConfig!.TempFolder, fileName);

            _logger!.LogInformation("Saving {EmailCount} emails for account {Mailbox} to {FileName}",
                emails.Count, account.Mailbox, fileName);

            // Ensure the temp directory exists
            if (!Directory.Exists(_appConfig.TempFolder))
            {
                Directory.CreateDirectory(_appConfig.TempFolder);
                _logger.LogInformation("Created temp directory: {TempFolder}", _appConfig.TempFolder);
            }

            // If the file already exists - delete it
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted existing file: {FilePath}", filePath);
            }

            // Serialize the emails to the file using a json serializer
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var jsonString = JsonSerializer.Serialize(emails, jsonOptions);
            File.WriteAllText(filePath, jsonString);

            _logger.LogInformation("Successfully saved emails to: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger!.LogError(ex, "Error saving emails for account {Mailbox} to JSON", account.Mailbox);
            throw; // Re-throw to maintain error handling behavior
        }
    }

}
