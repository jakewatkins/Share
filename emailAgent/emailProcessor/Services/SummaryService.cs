using System.Diagnostics;
using EmailProcessor.Models;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace EmailProcessor.Services;

/// <summary>
/// Service for generating email summaries using LLM
/// </summary>
public interface ISummaryService
{
    /// <summary>
    /// Gets a summary of an email using the configured LLM service
    /// </summary>
    /// <param name="email">The email to summarize</param>
    /// <returns>Email summary text</returns>
    Task<string> GetEmailSummaryAsync(ProcessedEmail email);
}

/// <summary>
/// Implementation of summary service using external LLM CLI tool
/// </summary>
public class SummaryService : ISummaryService
{
    private readonly ILogger<SummaryService> _logger;
    private readonly string _llmPath;
    private readonly string _summaryPrompt;
    private string? _promptFilePath;
    private string? _outputFilePath;

    /// <summary>
    /// Initializes a new instance of the SummaryService
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="llmPath">Path to the LLM command</param>
    /// <param name="summaryPrompt">Prompt template for summaries</param>
    public SummaryService(ILogger<SummaryService> logger, string llmPath, string summaryPrompt)
    {
        _logger = logger;
        _llmPath = llmPath;
        _summaryPrompt = summaryPrompt;
    }

    /// <summary>
    /// Gets a summary of an email using the configured LLM service
    /// </summary>
    /// <param name="email">The email to summarize</param>
    /// <returns>Email summary text</returns>
    public async Task<string> GetEmailSummaryAsync(ProcessedEmail email)
    {
        try
        {
            _logger.LogInformation("Generating summary for email {EmailId}", email.Id);

            // Clean HTML from subject and body
            var cleanSubject = RemoveHtml(email.Subject);
            var cleanBody = RemoveHtml(email.Body);

            // Create prompt file
            _promptFilePath = Path.Combine(Path.GetTempPath(), $"email-prompt-{Guid.NewGuid()}.txt");
            _outputFilePath = Path.Combine(Path.GetTempPath(), $"email-summary-{Guid.NewGuid()}.txt");

            var promptContent = $"{_summaryPrompt}\nsubject: {cleanSubject}\nbody: {cleanBody}";
            await File.WriteAllTextAsync(_promptFilePath, promptContent);

            // Build LLM command
            var llmExecutable = Path.Combine(_llmPath, "llm");
            var arguments = $"-PF \"{_promptFilePath}\" -o \"{_outputFilePath}\" --no-tools ibm-granite/granite-4.0-1b";

            _logger.LogDebug("Executing LLM command: {LlmPath} {Arguments}", llmExecutable, arguments);

            // Execute LLM command
            var processStartInfo = new ProcessStartInfo
            {
                FileName = llmExecutable,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process is null)
            {
                throw new InvalidOperationException("Failed to start LLM process");
            }

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new InvalidOperationException($"LLM process failed with exit code {process.ExitCode}: {error}");
            }

            // Read the output
            if (!File.Exists(_outputFilePath))
            {
                throw new InvalidOperationException("LLM did not create output file");
            }

            var summary = await File.ReadAllTextAsync(_outputFilePath);

            _logger.LogInformation("Successfully generated summary for email {EmailId}", email.Id);
            return summary.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating summary for email {EmailId}", email.Id);
            throw;
        }
        finally
        {
            // Clean up temp files
            CleanupTempFiles();
        }
    }

    /// <summary>
    /// Removes HTML tags from text content
    /// </summary>
    /// <param name="htmlContent">Content that may contain HTML</param>
    /// <returns>Plain text with HTML removed</returns>
    private static string RemoveHtml(string htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            return string.Empty;
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(htmlContent);

        // Get plain text and clean up whitespace
        var plainText = doc.DocumentNode.InnerText;
        return System.Text.RegularExpressions.Regex.Replace(plainText, @"\s+", " ").Trim();
    }

    /// <summary>
    /// Cleans up temporary files created during LLM processing
    /// </summary>
    private void CleanupTempFiles()
    {
        try
        {
            if (_promptFilePath is not null && File.Exists(_promptFilePath))
            {
                File.Delete(_promptFilePath);
                _logger.LogDebug("Deleted prompt file {PromptFile}", _promptFilePath);
            }

            if (_outputFilePath is not null && File.Exists(_outputFilePath))
            {
                File.Delete(_outputFilePath);
                _logger.LogDebug("Deleted output file {OutputFile}", _outputFilePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error cleaning up temp files");
        }
    }
}
