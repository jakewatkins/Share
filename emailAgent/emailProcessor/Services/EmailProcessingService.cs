using System.Text.Json;
using Azure.Data.Tables;
using EmailProcessor.Models;
using EmailProcessor.Services;
using Microsoft.Extensions.Logging;

namespace EmailProcessor.Services;

/// <summary>
/// Main service for processing email files and orchestrating email handling
/// </summary>
public interface IEmailProcessingService
{
    /// <summary>
    /// Processes all email files in the configured directory
    /// </summary>
    /// <returns>Task representing the async operation</returns>
    Task ProcessEmailsAsync();
}

/// <summary>
/// Implementation of email processing service
/// </summary>
public class EmailProcessingService : IEmailProcessingService
{
    private readonly ILogger<EmailProcessingService> _logger;
    private readonly IHandlerService _handlerService;
    private readonly ISummaryService _summaryService;
    private readonly IDailyReportService _dailyReportService;
    private readonly AppSettings _settings;

    /// <summary>
    /// Initializes a new instance of the EmailProcessingService
    /// </summary>
    public EmailProcessingService(
        ILogger<EmailProcessingService> logger,
        IHandlerService handlerService,
        ISummaryService summaryService,
        IDailyReportService dailyReportService,
        AppSettings settings)
    {
        _logger = logger;
        _handlerService = handlerService;
        _summaryService = summaryService;
        _dailyReportService = dailyReportService;
        _settings = settings;
    }

    /// <summary>
    /// Processes all email files in the configured directory
    /// </summary>
    public async Task ProcessEmailsAsync()
    {
        try
        {
            _logger.LogInformation("Starting email processing");

            // Initialize daily report
            await _dailyReportService.InitializeReportAsync(DateTime.Now);

            // Ensure directories exist
            EnsureDirectoriesExist();

            // Get all JSON files from ProcessedEmails directory
            var jsonFiles = Directory.GetFiles(_settings.ProcessedEmails, "*.json");

            if (jsonFiles.Length == 0)
            {
                _logger.LogInformation("No email files to process");
                return;
            }

            _logger.LogInformation("Found {FileCount} email files to process", jsonFiles.Length);

            // Process each file
            foreach (var filePath in jsonFiles)
            {
                await ProcessEmailFileAsync(filePath);
            }

            _logger.LogInformation("Email processing completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during email processing");
            throw;
        }
    }

    /// <summary>
    /// Processes a single email JSON file
    /// </summary>
    /// <param name="filePath">Path to the JSON file</param>
    private async Task ProcessEmailFileAsync(string filePath)
    {
        try
        {
            _logger.LogInformation("Processing email file: {FilePath}", filePath);

            // Read and parse JSON file
            var jsonContent = await File.ReadAllTextAsync(filePath);
            using var jsonDocument = JsonDocument.Parse(jsonContent);

            // Handle both array and single object formats
            var emails = new List<ProcessedEmail>();

            if (jsonDocument.RootElement.ValueKind == JsonValueKind.Array)
            {
                // Array of emails
                foreach (var emailElement in jsonDocument.RootElement.EnumerateArray())
                {
                    emails.Add(ProcessedEmail.FromJsonElement(emailElement));
                }
            }
            else
            {
                // Single email object
                emails.Add(ProcessedEmail.FromJsonElement(jsonDocument.RootElement));
            }

            _logger.LogInformation("Processing {EmailCount} emails from file {FilePath}", emails.Count, filePath);

            // Process each email
            foreach (var email in emails)
            {
                await ProcessSingleEmailAsync(email);
            }

            // Move file to archive
            await ArchiveFileAsync(filePath);

            _logger.LogInformation("Successfully processed file: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email file: {FilePath}", filePath);
            throw;
        }
    }

    /// <summary>
    /// Processes a single email according to the business rules
    /// </summary>
    /// <param name="email">The email to process</param>
    private async Task ProcessSingleEmailAsync(ProcessedEmail email)
    {
        try
        {
            _logger.LogInformation("Processing email {EmailId} from {FromAddress}", email.Id, email.From);

            // Check if email is in whitelist
            var whitelistEntry = await _handlerService.GetEmailWhiteListEntryAsync(email.From);

            if (whitelistEntry is not null)
            {
                _logger.LogInformation("Email {EmailId} from {FromAddress} is whitelisted", email.Id, email.From);

                // Handle whitelist entry
                await ProcessWhitelistEmailAsync(email, whitelistEntry);
            }
            else
            {
                _logger.LogInformation("Email {EmailId} from {FromAddress} not in whitelist, checking category handler", email.Id, email.From);

                // Check category handler
                await ProcessCategoryEmailAsync(email);
            }

            _logger.LogInformation("Successfully processed email {EmailId}", email.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email {EmailId}", email.Id);
            throw;
        }
    }

    /// <summary>
    /// Processes an email that's in the whitelist
    /// </summary>
    /// <param name="email">The email to process</param>
    /// <param name="whitelistEntry">The whitelist entry</param>
    private async Task ProcessWhitelistEmailAsync(ProcessedEmail email, EmailWhiteListEntity whitelistEntry)
    {
        // Include in report if specified
        if (whitelistEntry.IncludeInReport)
        {
            _logger.LogInformation("Including email {EmailId} in daily report", email.Id);
            var summary = await _summaryService.GetEmailSummaryAsync(email);
            await _dailyReportService.AddEmailToReportAsync(email, summary);
        }

        // Post to handler if URL is specified
        if (!string.IsNullOrWhiteSpace(whitelistEntry.HandlerUrl))
        {
            _logger.LogInformation("Posting email {EmailId} to whitelist handler {HandlerUrl}", email.Id, whitelistEntry.HandlerUrl);
            await _handlerService.PostEmailAsync(email, whitelistEntry.HandlerUrl);
        }
    }

    /// <summary>
    /// Processes an email using category handler
    /// </summary>
    /// <param name="email">The email to process</param>
    private async Task ProcessCategoryEmailAsync(ProcessedEmail email)
    {
        if (string.IsNullOrWhiteSpace(email.Classification))
        {
            _logger.LogWarning("Email {EmailId} has no classification, skipping category processing", email.Id);
            return;
        }

        var categoryHandler = await _handlerService.GetCategoryHandlerAsync(email.Classification);

        if (categoryHandler is not null && !string.IsNullOrWhiteSpace(categoryHandler.HandlerUrl))
        {
            _logger.LogInformation("Posting email {EmailId} to category handler {HandlerUrl}", email.Id, categoryHandler.HandlerUrl);
            await _handlerService.PostEmailAsync(email, categoryHandler.HandlerUrl);
        }
        else
        {
            _logger.LogWarning("No category handler found for classification {Classification} of email {EmailId}",
                email.Classification, email.Id);
        }
    }

    /// <summary>
    /// Archives a processed email file
    /// </summary>
    /// <param name="filePath">Path to the file to archive</param>
    private async Task ArchiveFileAsync(string filePath)
    {
        try
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var archiveFileName = $"{fileName}.bak";
            var archivePath = Path.Combine(_settings.EmailArchive, archiveFileName);

            // Delete existing archive file if it exists
            if (File.Exists(archivePath))
            {
                File.Delete(archivePath);
                _logger.LogInformation("Deleted existing archive file: {ArchivePath}", archivePath);
            }

            // Move file to archive
            File.Move(filePath, archivePath);
            _logger.LogInformation("Archived file from {SourcePath} to {ArchivePath}", filePath, archivePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving file: {FilePath}", filePath);
            throw;
        }
    }

    /// <summary>
    /// Ensures required directories exist
    /// </summary>
    private void EnsureDirectoriesExist()
    {
        var directories = new[]
        {
            _settings.ProcessedEmails,
            _settings.EmailArchive,
            _settings.DailyReportPath,
            _settings.EmailTemp
        };

        foreach (var directory in directories)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogInformation("Created directory: {Directory}", directory);
            }
        }
    }
}
