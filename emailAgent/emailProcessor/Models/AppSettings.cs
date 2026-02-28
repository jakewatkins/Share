namespace EmailProcessor.Models;

/// <summary>
/// Configuration settings for the email processor application
/// </summary>
public class AppSettings
{
    /// <summary>
    /// API key for handler service authentication
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Prompt template for LLM email summarization
    /// </summary>
    public string SummaryPrompt { get; set; } = string.Empty;

    /// <summary>
    /// Path to the LLM command-line tool
    /// </summary>
    public string LLMPath { get; set; } = string.Empty;

    /// <summary>
    /// Directory path where daily reports are stored
    /// </summary>
    public string DailyReportPath { get; set; } = string.Empty;

    /// <summary>
    /// Directory path for temporary email files
    /// </summary>
    public string EmailTemp { get; set; } = string.Empty;

    /// <summary>
    /// Directory path where processed email files are located
    /// </summary>
    public string ProcessedEmails { get; set; } = string.Empty;

    /// <summary>
    /// Directory path where email files are archived after processing
    /// </summary>
    public string EmailArchive { get; set; } = string.Empty;

    /// <summary>
    /// Azure Storage account connection string for tables
    /// </summary>
    public string StorageAccount { get; set; } = string.Empty;
}
