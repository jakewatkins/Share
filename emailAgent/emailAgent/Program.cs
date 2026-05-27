using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Extensions.Logging;
using EmailAgent.Core;
using EmailAgent.Entities;
using EmailAgent.Services;
using System.Text.Json;

namespace EmailAgent;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            // Create host with dependency injection
            var host = CreateHostBuilder(args).Build();

            // Get required services from DI container
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            var configuration = host.Services.GetRequiredService<IConfiguration>();
            var emailAccountProcessor = host.Services.GetRequiredService<EmailAccountProcessor>();

            logger.LogInformation("Email Agent starting up...");

            // Load application configuration
            var appConfig = new EmailAgentConfiguration();
            configuration.Bind(appConfig);

            // Validate configuration
            ValidateConfiguration(appConfig);

            logger.LogInformation("Loaded configuration for {AccountCount} email accounts", appConfig.EmailAccounts.Count);

            // Process email accounts
            foreach (var account in appConfig.EmailAccounts)
            {
                if (account.Enabled)
                {
                    Console.WriteLine($"Processing account: {account.Mailbox} ({account.Type})");
                    logger.LogInformation("Account configured: {Type} - {Mailbox}", account.Type, account.Mailbox);
                    var emails = await emailAccountProcessor.GetEmails(account);
                    if (emails != null)
                    {
                        SaveEmailAsJson(account, emails, appConfig.TempFolder, logger);
                    }
                }
                else
                {
                    logger.LogInformation("Skipping {Mailbox} - it has been disabled", account.Mailbox);
                }
            }

            logger.LogInformation("Email Agent completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex.Message}");
            Environment.Exit(1);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    /// <summary>
    /// Creates and configures the host builder with dependency injection
    /// </summary>
    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory())
                      .AddJsonFile("settings.json", optional: false, reloadOnChange: true)
                      .AddJsonFile("settings.development.json", optional: true, reloadOnChange: true)
                      .AddEnvironmentVariables();
            })
            .ConfigureLogging((context, logging) =>
            {
                // Clear default providers
                logging.ClearProviders();

                // Setup Serilog
                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(context.Configuration)
                    .CreateLogger();

                // Add Serilog to the logging pipeline
                logging.AddSerilog();
            })
            .ConfigureServices((context, services) =>
            {
                // Register KeyVault service
                services.AddSingleton<KeyVaultService>();

                // Register email account processor
                services.AddTransient<EmailAccountProcessor>();

                // Register AgentConfiguration as singleton since it loads secrets once
                services.AddSingleton<AgentConfiguration>();
            });

    /// <summary>
    /// Validates the application configuration
    /// </summary>
    private static void ValidateConfiguration(EmailAgentConfiguration appConfig)
    {
        if (string.IsNullOrWhiteSpace(appConfig.KeyvaultName))
            throw new InvalidOperationException("keyvaultName is required in configuration");

        if (string.IsNullOrWhiteSpace(appConfig.TempFolder))
            throw new InvalidOperationException("TempFolder is required in configuration");

        if (appConfig.EmailAccounts.Count == 0)
            throw new InvalidOperationException("At least one email account must be configured");

        foreach (var account in appConfig.EmailAccounts)
        {
            if (string.IsNullOrWhiteSpace(account.Type))
                throw new InvalidOperationException("Email account type is required");

            if (string.IsNullOrWhiteSpace(account.Mailbox))
                throw new InvalidOperationException("Email account mailbox is required");

            if (account.Type.ToLower() != "gmail" && account.Type.ToLower() != "outlook")
                throw new InvalidOperationException($"Unsupported email account type: {account.Type}");
        }
    }

    private static string GetMailBoxName(string emailAddress)
    {
        return emailAddress.Replace("@", "").Replace(".", "");
    }

    private static void SaveEmailAsJson(EmailAccount account, List<Email> emails, string tempFolder, Microsoft.Extensions.Logging.ILogger logger)
    {
        try
        {
            // Setup file name - TempFolder, the account name with the '@' and top level domain (ie '.com') removed, and the date time as "yyyyMMddHHmmss" with a json extension
            var sanitizedMailbox = GetMailBoxName(account.Mailbox);

            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var fileName = $"{sanitizedMailbox}-{timestamp}.json";
            var filePath = Path.Combine(tempFolder, fileName);

            logger.LogInformation("Saving {EmailCount} emails for account {Mailbox} to {FileName}",
                emails.Count, account.Mailbox, fileName);

            // Ensure the temp directory exists
            if (!Directory.Exists(tempFolder))
            {
                Directory.CreateDirectory(tempFolder);
                logger.LogInformation("Created temp directory: {TempFolder}", tempFolder);
            }

            // If the file already exists - delete it
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                logger.LogInformation("Deleted existing file: {FilePath}", filePath);
            }

            // Serialize the emails to the file using a json serializer
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var jsonString = JsonSerializer.Serialize(emails, jsonOptions);
            File.WriteAllText(filePath, jsonString);

            logger.LogInformation("Successfully saved emails to: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving emails for account {Mailbox} to JSON", account.Mailbox);
            throw; // Re-throw to maintain error handling behavior
        }
    }
}
