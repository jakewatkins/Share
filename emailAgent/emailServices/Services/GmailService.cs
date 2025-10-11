using EmailAgent.Core;
using EmailAgent.Entities;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Logging;
using System.Text;

namespace EmailAgent.Services
{
    public class GmailService : IDisposable
    {
        private readonly AgentConfiguration _configuration;
        private readonly ILogger _logger;
        private Google.Apis.Gmail.v1.GmailService? _gmailService;
        private DateTime _lastApiCall = DateTime.MinValue;
        private const int RATE_LIMIT_DELAY_MS = 100; // Minimum delay between API calls
        private bool _disposed = false;

        public GmailService(AgentConfiguration configuration, ILogger<GmailService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Validate required configuration values
            if (string.IsNullOrWhiteSpace(_configuration.GoogleCalendarId))
                throw new ArgumentException("GoogleCalendarId is required for Gmail service");
            if (string.IsNullOrWhiteSpace(_configuration.GoogleClientId))
                throw new ArgumentException("GoogleClientId is required for Gmail service");
            if (string.IsNullOrWhiteSpace(_configuration.GoogleClientSecret))
                throw new ArgumentException("GoogleClientSecret is required for Gmail service");

            _logger.LogInformation("Gmail Service initialized for email: {EmailAddress}", _configuration.GoogleCalendarId);
        }

        /// <summary>
        /// Ensures the Gmail service connection is ready and available
        /// </summary>
        /// <returns>The configured Gmail service instance</returns>
        /// <exception cref="InvalidOperationException">Thrown when service is disposed or connection failed</exception>
        private async Task<Google.Apis.Gmail.v1.GmailService> EnsureConnection()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GmailService));

            if (_gmailService == null)
            {
                await InitializeGmailService();
            }

            if (_gmailService == null)
                throw new InvalidOperationException("Gmail service connection is not available");

            return _gmailService;
        }

        public async Task<GetEmailResponse> GetEmail(GetEmailRequest request)
        {
            var response = new GetEmailResponse();
            
            try
            {
                _logger.LogInformation("Starting GetEmail request: NumberOfEmails={NumberOfEmails}", request.NumberOfEmails);

                // Ensure Gmail service connection is available
                var gmailService = await EnsureConnection();

                // Get messages from inbox
                var messages = await GetInboxMessages(gmailService, request.NumberOfEmails);

                // Convert to Email entities
                foreach (var message in messages)
                {
                    try
                    {
                        var email = await ConvertToEmail(gmailService, message);
                        response.Emails.Add(email);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error converting message {MessageId} to Email entity", message.Id);
                    }
                }

                _logger.LogInformation("Successfully retrieved {EmailCount} emails", response.Emails.Count);
                response.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving emails from Gmail");
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Deletes an email from the Gmail service
        /// </summary>
        /// <param name="email">Email entity containing the ID of the email to delete</param>
        /// <returns>True if email was successfully deleted, false otherwise</returns>
        /// <exception cref="ArgumentNullException">Thrown when email parameter is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when connection cannot be established</exception>
        /// <exception cref="GoogleApiException">Thrown when Gmail service returns an error</exception>
        public async Task<bool> DeleteEmail(Email email)
        {
            if (email == null)
            {
                _logger.LogError("DeleteEmail called with null email parameter");
                throw new ArgumentNullException(nameof(email), "Email parameter cannot be null");
            }

            // Validate that the email entity has a service type of Gmail
            if (email.Service != EmailService.Gmail)
            {
                _logger.LogWarning("Validation failure: Email ID {EmailId} has wrong service type {ServiceType}, expected Gmail", 
                    email.Id, email.Service);
                return false;
            }

            // Validate that the id value is not null or empty
            if (string.IsNullOrEmpty(email.Id))
            {
                _logger.LogWarning("Validation failure: Missing email ID for Gmail delete operation");
                return false;
            }

            try
            {
                // Ensure Gmail service connection is available
                var gmailService = await EnsureConnection();

                _logger.LogInformation("Attempting to delete email with ID: {EmailId}", email.Id);

                // Use the Email entity's id value to call the Gmail service's Messages.Delete method
                await RateLimitDelay();
                var deleteRequest = gmailService.Users.Messages.Delete("me", email.Id);
                await deleteRequest.ExecuteAsync();

                // If the Gmail service delete operation completes successfully return true
                _logger.LogInformation("Successfully deleted email with ID: {EmailId}", email.Id);
                return true;
            }
            catch (Google.GoogleApiException ex)
            {
                // If the Gmail service throws a GoogleApiException, log it and throw an exception
                _logger.LogError(ex, "GoogleApiException occurred while deleting email ID: {EmailId}", email.Id);
                throw;
            }
            catch (Exception ex)
            {
                // If the Gmail service returns any other error or exception, log the email's id and the error details and then return false
                _logger.LogWarning(ex, "Failed to delete email ID {EmailId}. Error details: {ErrorMessage}", 
                    email.Id, ex.Message);
                return false;
            }
        }

        private async Task InitializeGmailService()
        {
            try
            {
                _logger.LogInformation("Initializing Gmail service authentication");

                // Use GoogleWebAuthorizationBroker for authentication
                var userCredential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    new ClientSecrets
                    {
                        ClientId = _configuration.GoogleClientId,
                        ClientSecret = _configuration.GoogleClientSecret
                    },
                    new[] { Google.Apis.Gmail.v1.GmailService.Scope.GmailModify },
                    _configuration.GoogleCalendarId, // Using as user ID (email address)
                    CancellationToken.None);

                // Create Gmail service
                _gmailService = new Google.Apis.Gmail.v1.GmailService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = userCredential,
                    ApplicationName = "EmailAgent Gmail Service"
                });

                _logger.LogInformation("Gmail service initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Gmail service");
                throw new InvalidOperationException("Failed to authenticate with Gmail service", ex);
            }
        }

        private async Task<List<Message>> GetInboxMessages(Google.Apis.Gmail.v1.GmailService gmailService, int numberOfEmails)
        {
            _logger.LogInformation("Getting inbox messages: NumberOfEmails={NumberOfEmails}", numberOfEmails);

            var messages = new List<Message>();
            
            // Get message IDs from inbox
            var request = gmailService.Users.Messages.List("me");
            request.Q = "in:inbox";
            request.MaxResults = numberOfEmails;
            
            await RateLimitDelay();
            var response = await request.ExecuteAsync();
            
            if (response.Messages == null || response.Messages.Count == 0)
            {
                _logger.LogInformation("No messages found in inbox");
                return messages;
            }

            _logger.LogInformation("Found {MessageCount} messages in inbox", response.Messages.Count);

            // Get full message details for each message (retrieve oldest first)
            var messageIds = response.Messages.OrderBy(m => m.Id).Take(numberOfEmails).ToList();
            
            foreach (var messageId in messageIds)
            {
                await RateLimitDelay();
                var messageRequest = gmailService.Users.Messages.Get("me", messageId.Id);
                messageRequest.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;
                
                var message = await messageRequest.ExecuteAsync();
                messages.Add(message);
            }

            return messages;
        }

        private async Task<Email> ConvertToEmail(Google.Apis.Gmail.v1.GmailService gmailService, Message gmailMessage)
        {
            var email = new Email
            {
                Id = gmailMessage.Id ?? string.Empty,
                Service = EmailService.Gmail
            };

            // Extract headers
            if (gmailMessage.Payload?.Headers != null)
            {
                foreach (var header in gmailMessage.Payload.Headers)
                {
                    switch (header.Name?.ToLowerInvariant())
                    {
                        case "from":
                            email.From = header.Value ?? string.Empty;
                            break;
                        case "to":
                            email.To = ParseEmailAddresses(header.Value ?? string.Empty);
                            break;
                        case "cc":
                            email.CC = ParseEmailAddresses(header.Value ?? string.Empty);
                            break;
                        case "bcc":
                            email.BCC = ParseEmailAddresses(header.Value ?? string.Empty);
                            break;
                        case "subject":
                            email.Subject = header.Value ?? string.Empty;
                            break;
                        case "date":
                            if (DateTime.TryParse(header.Value, out var sentDate))
                            {
                                email.SentDateTime = sentDate;
                            }
                            break;
                    }
                }
            }

            // Parse message parts for body and attachments
            if (gmailMessage.Payload != null)
            {
                var bodyContent = await ParseMessagePart(gmailMessage.Payload, email, gmailMessage.Id!);
                
                // Favor HTML body over plain text
                email.Body = !string.IsNullOrWhiteSpace(bodyContent.HtmlBody) ? bodyContent.HtmlBody : bodyContent.PlainBody;
            }

            return email;
        }

        private async Task<(string HtmlBody, string PlainBody)> ParseMessagePart(MessagePart part, Email email, string messageId)
        {
            string htmlBody = string.Empty;
            string plainBody = string.Empty;
            
            if (part.Parts != null && part.Parts.Count > 0)
            {
                // Multipart message
                foreach (var subPart in part.Parts)
                {
                    var subResult = await ParseMessagePart(subPart, email, messageId);
                    if (!string.IsNullOrWhiteSpace(subResult.HtmlBody))
                        htmlBody = subResult.HtmlBody;
                    if (!string.IsNullOrWhiteSpace(subResult.PlainBody))
                        plainBody = subResult.PlainBody;
                }
            }
            else
            {
                // Single part
                var mimeType = part.MimeType?.ToLowerInvariant();
                
                if (mimeType == "text/plain")
                {
                    plainBody = GetMessagePartContent(part);
                }
                else if (mimeType == "text/html")
                {
                    htmlBody = GetMessagePartContent(part);
                }
                else if (!string.IsNullOrEmpty(part.Filename))
                {
                    // Attachment - only retrieve metadata
                    var attachment = CreateAttachment(part);
                    if (attachment != null)
                    {
                        email.Attachments.Add(attachment);
                    }
                }
            }
            
            return (htmlBody, plainBody);
        }

        private string GetMessagePartContent(MessagePart part)
        {
            if (part.Body?.Data != null)
            {
                try
                {
                    var data = Convert.FromBase64String(part.Body.Data.Replace('-', '+').Replace('_', '/'));
                    return Encoding.UTF8.GetString(data);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to decode message part content");
                }
            }
            return string.Empty;
        }

        private EmailAttachment? CreateAttachment(MessagePart part)
        {
            try
            {
                var attachment = new EmailAttachment
                {
                    Name = part.Filename ?? "unknown",
                    Type = part.MimeType ?? "application/octet-stream",
                    Size = part.Body?.Size ?? 0
                };

                _logger.LogDebug("Created attachment metadata: Name={Name}, Type={Type}, Size={Size}", 
                    attachment.Name, attachment.Type, attachment.Size);

                return attachment;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create attachment from message part");
                return null;
            }
        }

        private async Task RateLimitDelay()
        {
            var timeSinceLastCall = DateTime.Now - _lastApiCall;
            var remainingDelay = TimeSpan.FromMilliseconds(RATE_LIMIT_DELAY_MS) - timeSinceLastCall;
            
            if (remainingDelay > TimeSpan.Zero)
            {
                await Task.Delay(remainingDelay);
            }
            
            _lastApiCall = DateTime.Now;
        }

        private List<string> ParseEmailAddresses(string emailAddresses)
        {
            if (string.IsNullOrWhiteSpace(emailAddresses))
                return new List<string>();

            // Split by comma and clean up each address
            return emailAddresses
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(addr => addr.Trim())
                .Where(addr => !string.IsNullOrWhiteSpace(addr))
                .ToList();
        }

        /// <summary>
        /// Releases all resources used by the GmailService
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the GmailService and optionally releases the managed resources
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _gmailService?.Dispose();
                    _logger.LogDebug("Gmail Service disposed");
                }

                _disposed = true;
            }
        }
    }
}
