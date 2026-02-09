using Azure.Data.Tables;
using EmailProcessor.Models;
using EmailProcessor.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace EmailProcessor;

/// <summary>
/// Main entry point for the Email Processor application
/// </summary>
public class Program
{
    /// <summary>
    /// Main entry point
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Exit code</returns>
    public static async Task<int> Main(string[] args)
    {
        try
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("settings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            Log.Information("Starting Email Processor application");

            // Build host
            var host = CreateHostBuilder(args, configuration).Build();

            // Run the application
            using (host)
            {
                var emailProcessor = host.Services.GetRequiredService<IEmailProcessingService>();
                await emailProcessor.ProcessEmailsAsync();
            }

            Log.Information("Email Processor completed successfully");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"HELP: An error occurred during email processing.");
            Console.WriteLine($"Error Details: {ex.Message}");

            if (ex.InnerException is not null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }

            Console.WriteLine($"Stack Trace: {ex.StackTrace}");

            Log.Fatal(ex, "Email Processor terminated unexpectedly");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    /// <summary>
    /// Creates the host builder for dependency injection
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <param name="configuration">Configuration instance</param>
    /// <returns>Host builder</returns>
    private static IHostBuilder CreateHostBuilder(string[] args, IConfiguration configuration) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Bind configuration
                var settings = new AppSettings();
                configuration.Bind(settings);

                // Validate required settings
                ValidateSettings(settings);

                services.AddSingleton(settings);

                // Configure logging
                services.AddLogging(builder =>
                {
                    builder.ClearProviders();
                    builder.AddSerilog();
                });

                // Configure Azure Table Storage
                services.AddSingleton(serviceProvider =>
                {
                    return new TableServiceClient(settings.StorageAccount);
                });

                // Configure HTTP client
                services.AddHttpClient<IHandlerService, HandlerService>(client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                });

                // Register services
                services.AddScoped<IHandlerService>(serviceProvider =>
                {
                    var httpClient = serviceProvider.GetRequiredService<HttpClient>();
                    var logger = serviceProvider.GetRequiredService<ILogger<HandlerService>>();
                    var tableServiceClient = serviceProvider.GetRequiredService<TableServiceClient>();
                    return new HandlerService(httpClient, logger, tableServiceClient, settings.ApiKey);
                });

                services.AddScoped<ISummaryService>(serviceProvider =>
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<SummaryService>>();
                    return new SummaryService(logger, settings.LLMPath, settings.SummaryPrompt);
                });

                services.AddScoped<IDailyReportService>(serviceProvider =>
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<DailyReportService>>();
                    return new DailyReportService(logger, settings.DailyReportPath);
                });

                services.AddScoped<IEmailProcessingService, EmailProcessingService>();
            })
            .UseSerilog();

    /// <summary>
    /// Validates required configuration settings
    /// </summary>
    /// <param name="settings">Settings to validate</param>
    private static void ValidateSettings(AppSettings settings)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            errors.Add("API-KEY is required in configuration");

        if (string.IsNullOrWhiteSpace(settings.SummaryPrompt))
            errors.Add("summaryPrompt is required in configuration");

        if (string.IsNullOrWhiteSpace(settings.LLMPath))
            errors.Add("LLMPath is required in configuration");

        if (string.IsNullOrWhiteSpace(settings.DailyReportPath))
            errors.Add("DailyReportPath is required in configuration");

        if (string.IsNullOrWhiteSpace(settings.ProcessedEmails))
            errors.Add("ProcessedEmails is required in configuration");

        if (string.IsNullOrWhiteSpace(settings.EmailArchive))
            errors.Add("EmailArchive is required in configuration");

        if (string.IsNullOrWhiteSpace(settings.StorageAccount))
            errors.Add("StorageAccount is required in configuration");

        if (errors.Count > 0)
        {
            var errorMessage = "Configuration validation failed:\n" + string.Join("\n", errors);
            throw new InvalidOperationException(errorMessage);
        }
    }
}
