using EmailAgent.Core;
using EmailAgent.Entities;
using EmailAgent.Services;
using EmailServices.Api.Services;
using Microsoft.Extensions.Logging;

namespace EmailServices.Api.Services;

public class EmailOperationsService : IEmailOperationsService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailOperationsService> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public EmailOperationsService(
        IConfiguration configuration,
        ILogger<EmailOperationsService> logger,
        ILoggerFactory loggerFactory)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public async Task<GetEmailResponse> GetEmailsAsync(string service, string userEmail, int count = 10)
    {
        try
        {
            _logger.LogInformation("Retrieving {Count} emails from {Service} for user {UserEmail}", count, service, userEmail);

            if (!Enum.TryParse<EmailService>(service, true, out var emailService))
            {
                _logger.LogError("Invalid email service specified: {Service}", service);
                throw new ArgumentException($"Invalid email service: {service}", nameof(service));
            }

            var request = new GetEmailRequest(0, count);

            return emailService switch
            {
                EmailService.Gmail => await GetGmailEmailsAsync(request, userEmail),
                EmailService.Outlook => await GetOutlookEmailsAsync(request, userEmail),
                _ => throw new NotSupportedException($"Email service {emailService} is not supported for retrieval")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving emails from {Service} for user {UserEmail}", service, userEmail);
            throw;
        }
    }

    public async Task<bool> DeleteEmailAsync(string emailId, string service, string userEmail)
    {
        try
        {
            _logger.LogInformation("Attempting to delete email {EmailId} from {Service} for user {UserEmail}",
                emailId, service, userEmail);

            // Parse service type
            if (!Enum.TryParse<EmailService>(service, true, out var emailService))
            {
                _logger.LogError("Invalid email service specified: {Service}", service);
                throw new ArgumentException($"Invalid email service: {service}", nameof(service));
            }

            // Create email object for deletion
            var email = new Email
            {
                Id = emailId,
                Service = emailService
            };

            // Route to appropriate service
            var success = emailService switch
            {
                EmailService.Gmail => await DeleteGmailAsync(email, userEmail),
                EmailService.Outlook => await DeleteOutlookAsync(email, userEmail),
                _ => throw new NotSupportedException($"Email service {emailService} is not supported for deletion")
            };

            _logger.LogInformation("Email deletion {Result} for {EmailId}",
                success ? "succeeded" : "failed", emailId);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting email {EmailId} from {Service} for user {UserEmail}",
                emailId, service, userEmail);
            throw;
        }
    }

    public async Task<bool> MoveEmailAsync(string emailId, string service, string userEmail, string destinationFolder)
    {
        try
        {
            _logger.LogInformation("Attempting to move email {EmailId} from {Service} to folder {Folder} for user {UserEmail}",
                emailId, service, destinationFolder, userEmail);

            // Parse service type
            if (!Enum.TryParse<EmailService>(service, true, out var emailService))
            {
                _logger.LogError("Invalid email service specified: {Service}", service);
                throw new ArgumentException($"Invalid email service: {service}", nameof(service));
            }

            // Route to appropriate service
            var success = emailService switch
            {
                EmailService.Gmail => await MoveGmailAsync(emailId, userEmail, destinationFolder),
                EmailService.Outlook => await MoveOutlookAsync(emailId, userEmail, destinationFolder),
                _ => throw new NotSupportedException($"Email service {emailService} is not supported for moving")
            };

            _logger.LogInformation("Email move {Result} for {EmailId}",
                success ? "succeeded" : "failed", emailId);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving email {EmailId} from {Service} for user {UserEmail}",
                emailId, service, userEmail);
            throw;
        }
    }

    private async Task<GetEmailResponse> GetGmailEmailsAsync(GetEmailRequest request, string userEmail)
    {
        var keyVaultLogger = _loggerFactory.CreateLogger<KeyVaultService>();
        var keyVaultService = new KeyVaultService(_configuration, keyVaultLogger);

        var gmailLogger = _loggerFactory.CreateLogger<GmailService>();
        using var gmailService = new GmailService(_configuration, keyVaultService, gmailLogger, userEmail);

        return await gmailService.GetEmail(request);
    }

    private async Task<GetEmailResponse> GetOutlookEmailsAsync(GetEmailRequest request, string userEmail)
    {
        var agentConfig = new AgentConfiguration(_configuration);
        var keyVaultLogger = _loggerFactory.CreateLogger<KeyVaultService>();
        var keyVaultService = new KeyVaultService(_configuration, keyVaultLogger);

        var outlookLogger = _loggerFactory.CreateLogger<OutlookService>();
        using var outlookService = new OutlookService(agentConfig, keyVaultService, outlookLogger, userEmail);

        return await outlookService.GetEmail(request);
    }

    private async Task<bool> DeleteGmailAsync(Email email, string userEmail)
    {
        // Create Key Vault service for token access
        var keyVaultLogger = _loggerFactory.CreateLogger<KeyVaultService>();
        var keyVaultService = new KeyVaultService(_configuration, keyVaultLogger);

        // Create Gmail service instance
        var gmailLogger = _loggerFactory.CreateLogger<GmailService>();
        using var gmailService = new GmailService(_configuration, keyVaultService, gmailLogger, userEmail);

        return await gmailService.DeleteEmail(email);
    }

    private async Task<bool> DeleteOutlookAsync(Email email, string userEmail)
    {
        // Create AgentConfiguration and Key Vault service for token access
        var agentConfig = new AgentConfiguration(_configuration);
        var keyVaultLogger = _loggerFactory.CreateLogger<KeyVaultService>();
        var keyVaultService = new KeyVaultService(_configuration, keyVaultLogger);

        // Create Outlook service instance - Outlook uses AgentConfiguration
        var outlookLogger = _loggerFactory.CreateLogger<OutlookService>();
        using var outlookService = new OutlookService(agentConfig, keyVaultService, outlookLogger, userEmail);

        return await outlookService.DeleteEmail(email);
    }

    private async Task<bool> MoveGmailAsync(string emailId, string userEmail, string destinationLabel)
    {
        // TODO: Implement Gmail move functionality using labels
        // Gmail doesn't have folders - uses labels for organization
        _logger.LogWarning("Gmail move operation not yet implemented for email {EmailId}", emailId);
        throw new NotImplementedException("Gmail move operation is not yet implemented");
    }

    private async Task<bool> MoveOutlookAsync(string emailId, string userEmail, string destinationFolder)
    {
        // TODO: Implement Outlook move functionality using Microsoft Graph API
        // Move to specified folder by ID or name
        _logger.LogWarning("Outlook move operation not yet implemented for email {EmailId}", emailId);
        throw new NotImplementedException("Outlook move operation is not yet implemented");
    }
}
