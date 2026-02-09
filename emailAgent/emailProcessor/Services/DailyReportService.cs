using EmailProcessor.Models;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Web;

namespace EmailProcessor.Services;

/// <summary>
/// Service for generating daily email reports in HTML format
/// </summary>
public interface IDailyReportService
{
    /// <summary>
    /// Initializes a new daily report, clearing any existing report
    /// </summary>
    /// <param name="reportDate">Date for the report</param>
    Task InitializeReportAsync(DateTime reportDate);

    /// <summary>
    /// Adds an email entry to the daily report
    /// </summary>
    /// <param name="email">The email to add</param>
    /// <param name="summary">Optional summary of the email</param>
    Task AddEmailToReportAsync(ProcessedEmail email, string? summary = null);

    /// <summary>
    /// Gets the current report file path
    /// </summary>
    /// <returns>Path to the current report file</returns>
    string GetReportFilePath(DateTime reportDate);
}

/// <summary>
/// Implementation of daily report service for HTML report generation
/// </summary>
public class DailyReportService : IDailyReportService
{
    private readonly ILogger<DailyReportService> _logger;
    private readonly string _dailyReportPath;
    private string? _currentReportPath;

    /// <summary>
    /// Initializes a new instance of the DailyReportService
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="dailyReportPath">Directory path for daily reports</param>
    public DailyReportService(ILogger<DailyReportService> logger, string dailyReportPath)
    {
        _logger = logger;
        _dailyReportPath = dailyReportPath;
    }

    /// <summary>
    /// Initializes a new daily report, clearing any existing report
    /// </summary>
    /// <param name="reportDate">Date for the report</param>
    public async Task InitializeReportAsync(DateTime reportDate)
    {
        try
        {
            // Ensure report directory exists
            if (!Directory.Exists(_dailyReportPath))
            {
                Directory.CreateDirectory(_dailyReportPath);
                _logger.LogInformation("Created daily report directory: {ReportPath}", _dailyReportPath);
            }

            // Generate report file path
            _currentReportPath = GetReportFilePath(reportDate);

            // Delete existing report file if it exists
            if (File.Exists(_currentReportPath))
            {
                File.Delete(_currentReportPath);
                _logger.LogInformation("Deleted existing daily report file: {ReportPath}", _currentReportPath);
            }

            // Create initial HTML structure
            var initialHtml = CreateInitialHtml();
            await File.WriteAllTextAsync(_currentReportPath, initialHtml);

            _logger.LogInformation("Initialized daily report: {ReportPath}", _currentReportPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing daily report for date {ReportDate}", reportDate);
            throw;
        }
    }

    /// <summary>
    /// Adds an email entry to the daily report
    /// </summary>
    /// <param name="email">The email to add</param>
    /// <param name="summary">Optional summary of the email</param>
    public async Task AddEmailToReportAsync(ProcessedEmail email, string? summary = null)
    {
        if (_currentReportPath is null)
        {
            throw new InvalidOperationException("Report not initialized. Call InitializeReportAsync first.");
        }

        try
        {
            _logger.LogInformation("Adding email {EmailId} to daily report", email.Id);

            // Generate email URL based on service
            var emailUrl = GenerateEmailUrl(email);

            // Create table row HTML
            var rowHtml = CreateEmailRowHtml(email.Subject, emailUrl, summary ?? string.Empty);

            // Read current content
            var currentContent = await File.ReadAllTextAsync(_currentReportPath);

            // Insert the new row before the closing table tag
            var updatedContent = currentContent.Replace("</table>", $"{rowHtml}</table>");

            // Write back to file
            await File.WriteAllTextAsync(_currentReportPath, updatedContent);

            _logger.LogInformation("Successfully added email {EmailId} to daily report", email.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding email {EmailId} to daily report", email.Id);
            throw;
        }
    }

    /// <summary>
    /// Gets the current report file path
    /// </summary>
    /// <returns>Path to the current report file</returns>
    public string GetReportFilePath(DateTime reportDate)
    {
        var fileName = $"email-report-{reportDate:yyyyMMdd}.html";
        return Path.Combine(_dailyReportPath, fileName);
    }

    /// <summary>
    /// Creates the initial HTML structure for the daily report
    /// </summary>
    /// <returns>Initial HTML content</returns>
    private static string CreateInitialHtml()
    {
        return """
<!DOCTYPE html>
<html>
<head>
    <title>Daily Email Report</title>
</head>
<body>
    <h1>Daily Email Report</h1>
    <table>
        <tr><th>Email Subject</th><th>Email Summary</th></tr>
        <tr><td></td><td></td></tr>
    </table>
</body>
</html>
""";
    }

    /// <summary>
    /// Creates HTML for an email row in the report table
    /// </summary>
    /// <param name="subject">Email subject</param>
    /// <param name="emailUrl">URL to the email</param>
    /// <param name="summary">Email summary</param>
    /// <returns>HTML table row</returns>
    private static string CreateEmailRowHtml(string subject, string emailUrl, string summary)
    {
        var escapedSubject = System.Web.HttpUtility.HtmlEncode(subject);
        var escapedSummary = System.Web.HttpUtility.HtmlEncode(summary);
        var escapedUrl = System.Web.HttpUtility.HtmlAttributeEncode(emailUrl);

        return $"        <tr><td><a href=\"{escapedUrl}\">{escapedSubject}</a></td><td>{escapedSummary}</td></tr>\n";
    }

    /// <summary>
    /// Generates the appropriate URL for an email based on its service
    /// </summary>
    /// <param name="email">The email to generate URL for</param>
    /// <returns>URL string for the email</returns>
    private static string GenerateEmailUrl(ProcessedEmail email)
    {
        return email.Service.ToString().ToLowerInvariant() switch
        {
            "gmail" => $"https://mail.google.com/mail/u/0/#inbox/{email.Id}",
            "outlook" => $"https://outlook.live.com/mail/0/inbox/id/{email.Id}",
            _ => $"https://outlook.live.com/mail/0/inbox/id/{email.Id}" // Default to Outlook format
        };
    }
}
