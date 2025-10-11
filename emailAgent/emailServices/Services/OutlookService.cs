using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using Microsoft.Extensions.Logging;
using EmailAgent.Core;
using EmailAgent.Entities;

namespace EmailAgent.Services
{
    /// <summary>
    /// Service for retrieving emails from Microsoft Outlook using Microsoft Graph API
    /// </summary>
    public class OutlookService : IDisposable
    {
        private readonly AgentConfiguration _configuration;
        private readonly ILogger _logger;
        private GraphServiceClient _graphClient = null!; // Initialized in constructor via InitializeGraphClient
        private readonly string[] _scopes = { "Mail.Read", "Mail.ReadWrite" };
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the OutlookService
        /// </summary>
        /// <param name="configuration">Agent configuration containing Outlook settings</param>
        /// <param name="logger">Logger for diagnostic information</param>
        /// <exception cref="ArgumentNullException">Thrown when configuration or logger is null</exception>
        /// <exception cref="ArgumentException">Thrown when required Outlook configuration values are missing</exception>
        public OutlookService(AgentConfiguration configuration, ILogger logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Validate required configuration values
            if (string.IsNullOrWhiteSpace(_configuration.OutlookClientId))
                throw new ArgumentException("Outlook Client ID is required", nameof(configuration));
            
            if (string.IsNullOrWhiteSpace(_configuration.OutlookSecret))
                throw new ArgumentException("Outlook Secret is required", nameof(configuration));

            try
            {
                // Initialize Microsoft Graph client
                InitializeGraphClient();
                
                _logger.LogInformation("Outlook Service initialized with Client ID: {ClientId}", 
                    _configuration.OutlookClientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Outlook Service");
                throw new InvalidOperationException("Failed to initialize Outlook Service", ex);
            }
        }

        /// <summary>
        /// Ensures the Graph service client connection is ready and available
        /// </summary>
        /// <returns>The configured GraphServiceClient instance</returns>
        /// <exception cref="InvalidOperationException">Thrown when service is disposed or connection failed</exception>
        private GraphServiceClient EnsureConnection()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(OutlookService));

            if (_graphClient == null)
                throw new InvalidOperationException("Graph service client connection is not available");

            return _graphClient;
        }

        /// <summary>
        /// Retrieves emails from the Outlook service
        /// </summary>
        /// <param name="request">Request containing email retrieval parameters</param>
        /// <returns>Response containing retrieved emails or error information</returns>
        public async Task<GetEmailResponse> GetEmail(GetEmailRequest request)
        {
            var response = new GetEmailResponse
            {
                Emails = new List<Email>(),
                Success = false,
                Message = string.Empty,
                Service = EmailService.Outlook
            };

            try
            {
                if (request == null)
                {
                    throw new ArgumentNullException(nameof(request), "GetEmailRequest cannot be null");
                }

                _logger.LogInformation("Starting email retrieval for {NumberOfEmails} emails", request.NumberOfEmails);

                // Ensure Graph client connection is available
                var graphClient = EnsureConnection();

                // Get emails from inbox, ordered by ReceivedDateTime (oldest first)
                var messages = await graphClient.Me.MailFolders.Inbox.Messages
                    .Request()
                    .OrderBy("receivedDateTime asc")
                    .Skip(request.StartIndex)
                    .Top(request.NumberOfEmails)
                    .Expand("attachments")
                    .GetAsync();

                _logger.LogInformation("Found {EmailCount} emails in inbox", messages.Count);

                int processedCount = 0;

                // Process each email
                foreach (var message in messages)
                {
                    try
                    {
                        var email = await ConvertToEmailAsync(message);
                        response.Emails.Add(email);
                        processedCount++;
                        
                        _logger.LogDebug("Processed email: {Subject} from {From}", 
                            email.Subject, email.From);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing email with ID: {MessageId}", message.Id);
                        // Continue processing other emails
                    }
                }

                response.Success = true;
                response.Count = processedCount;
                response.Message = processedCount < request.NumberOfEmails ? "Last batch retrieved" : "ok";
                
                _logger.LogInformation("Successfully retrieved {EmailCount} emails", processedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving emails from Outlook service");
                response.Success = false;
                response.Message = $"Failed to retrieve emails: {ex.Message}";
                
                // If it's an authentication failure, provide more specific information
                if (ex.Message.Contains("Unauthorized") || ex.Message.Contains("401") || 
                    ex.Message.Contains("authentication") || ex.Message.Contains("token"))
                {
                    response.Message = "Authentication failed. Please check Outlook credentials and ensure proper consent.";
                }
            }

            return response;
        }

        /// <summary>
        /// Deletes an email from the Outlook service
        /// </summary>
        /// <param name="email">Email entity containing the ID of the email to delete</param>
        /// <returns>True if email was successfully deleted, false otherwise</returns>
        /// <exception cref="ArgumentNullException">Thrown when email parameter is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when connection cannot be established</exception>
        /// <exception cref="ServiceException">Thrown when Microsoft Graph service returns an error</exception>
        public async Task<bool> DeleteEmail(Email email)
        {
            if (email == null)
            {
                _logger.LogError("DeleteEmail called with null email parameter");
                throw new ArgumentNullException(nameof(email), "Email parameter cannot be null");
            }

            // Validate that the email entity has a service type of Outlook
            if (email.Service != EmailService.Outlook)
            {
                _logger.LogWarning("Validation failure: Email ID {EmailId} has wrong service type {ServiceType}, expected Outlook", 
                    email.Id, email.Service);
                return false;
            }

            // Validate that the id value is not null or empty
            if (string.IsNullOrEmpty(email.Id))
            {
                _logger.LogWarning("Validation failure: Missing email ID for Outlook delete operation");
                return false;
            }

            try
            {
                // Ensure Graph service client connection is available
                var graphClient = EnsureConnection();

                _logger.LogInformation("Attempting to delete email with ID: {EmailId}", email.Id);

                // Use the Email entity's id value to call the Microsoft Graph API's Messages.Delete method
                await graphClient.Me.Messages[email.Id]
                    .Request()
                    .DeleteAsync();

                // If the Microsoft Graph service delete operation completes successfully return true
                _logger.LogInformation("Successfully deleted email with ID: {EmailId}", email.Id);
                return true;
            }
            catch (ServiceException ex)
            {
                // If the Microsoft Graph service throws a ServiceException, log it and throw an exception
                _logger.LogError(ex, "ServiceException occurred while deleting email ID: {EmailId}", email.Id);
                throw;
            }
            catch (Exception ex)
            {
                // If the Microsoft Graph service returns any other error or exception, log the email's id and the error details and then return false
                _logger.LogWarning(ex, "Failed to delete email ID {EmailId}. Error details: {ErrorMessage}", 
                    email.Id, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Initializes the Microsoft Graph client with authentication
        /// </summary>
        private void InitializeGraphClient()
        {
            // Create the public client application for delegated permissions
            var app = PublicClientApplicationBuilder
                .Create(_configuration.OutlookClientId)
                .WithAuthority("https://login.microsoftonline.com/common")
                .WithRedirectUri("http://localhost")
                .Build();

            _logger.LogDebug("Created PublicClientApplication with Client ID: {ClientId}", _configuration.OutlookClientId);

            // Create Graph client with InteractiveAuthenticationProvider
            var authProvider = new InteractiveAuthenticationProvider(app, _scopes);
            _graphClient = new GraphServiceClient(authProvider);

            _logger.LogDebug("Initialized GraphServiceClient with scopes: {Scopes}", string.Join(", ", _scopes));
        }

        /// <summary>
        /// Converts a Microsoft Graph Message to the unified Email entity
        /// </summary>
        /// <param name="message">Microsoft Graph Message</param>
        /// <returns>Converted Email entity</returns>
        private async Task<Email> ConvertToEmailAsync(Message message)
        {
            var email = new Email
            {
                Id = message.Id ?? Guid.NewGuid().ToString(),
                Service = EmailService.Outlook,
                From = message.From?.EmailAddress?.Address ?? string.Empty,
                To = message.ToRecipients?.Select(r => r.EmailAddress?.Address ?? string.Empty).ToList() ?? new List<string>(),
                CC = message.CcRecipients?.Select(r => r.EmailAddress?.Address ?? string.Empty).ToList() ?? new List<string>(),
                BCC = message.BccRecipients?.Select(r => r.EmailAddress?.Address ?? string.Empty).ToList() ?? new List<string>(),
                SentDateTime = message.SentDateTime?.DateTime ?? DateTime.UtcNow,
                Subject = message.Subject ?? string.Empty,
                Body = GetPreferredBodyContent(message.Body),
                Attachments = new List<EmailAttachment>()
            };

            // Process attachments (metadata only as per requirements)
            if (message.Attachments?.Any() == true)
            {
                foreach (var attachment in message.Attachments)
                {
                    try
                    {
                        var emailAttachment = new EmailAttachment
                        {
                            Name = attachment.Name ?? "Unknown",
                            Type = GetFileExtension(attachment.Name),
                            Size = (int)(attachment.Size ?? 0),
                            Content = null // Only retrieve metadata, not content
                        };

                        email.Attachments.Add(emailAttachment);

                        _logger.LogDebug("Mapped attachment: {Name} ({Size} bytes, type: {Type})", 
                            emailAttachment.Name, emailAttachment.Size, emailAttachment.Type);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to process attachment: {AttachmentName}", attachment.Name);
                        // Continue processing other attachments
                    }
                }
            }

            return await Task.FromResult(email);
        }

        /// <summary>
        /// Gets the preferred body content, favoring HTML over plain text
        /// </summary>
        /// <param name="body">Message body</param>
        /// <returns>Body content as string</returns>
        private string GetPreferredBodyContent(ItemBody? body)
        {
            if (body == null)
                return string.Empty;

            // Favor HTML over plain text as specified in requirements
            if (body.ContentType == BodyType.Html)
            {
                return body.Content ?? string.Empty;
            }
            else if (body.ContentType == BodyType.Text)
            {
                return body.Content ?? string.Empty;
            }

            return body.Content ?? string.Empty;
        }

        /// <summary>
        /// Extracts file extension from filename
        /// </summary>
        /// <param name="filename">Filename</param>
        /// <returns>File extension without dot, or "unknown" if not found</returns>
        private string GetFileExtension(string? filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                return "unknown";

            var lastDotIndex = filename.LastIndexOf('.');
            if (lastDotIndex >= 0 && lastDotIndex < filename.Length - 1)
            {
                return filename.Substring(lastDotIndex + 1).ToLowerInvariant();
            }

            return "unknown";
        }

        /// <summary>
        /// Releases all resources used by the OutlookService
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the OutlookService and optionally releases the managed resources
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // GraphServiceClient doesn't implement IDisposable in this version
                    // We can clear the reference
                    _graphClient = null!;
                    _logger.LogDebug("Outlook Service disposed");
                }

                _disposed = true;
            }
        }
    }
}
