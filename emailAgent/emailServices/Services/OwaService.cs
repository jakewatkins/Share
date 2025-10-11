using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.Extensions.Logging;
using EmailAgent.Core;
using EmailAgent.Entities;

namespace EmailAgent.Services
{
    /// <summary>
    /// Service for retrieving emails from Microsoft Exchange/OWA using Exchange Web Services
    /// </summary>
    public class OwaService : IDisposable
    {
        private readonly AgentConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly ExchangeService _exchangeService;
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the OwaService
        /// </summary>
        /// <param name="configuration">Agent configuration containing OWA settings</param>
        /// <param name="logger">Logger for diagnostic information</param>
        /// <exception cref="ArgumentNullException">Thrown when configuration or logger is null</exception>
        /// <exception cref="ArgumentException">Thrown when required OWA configuration values are missing</exception>
        public OwaService(AgentConfiguration configuration, ILogger logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Validate required configuration values
            if (string.IsNullOrWhiteSpace(_configuration.OwaEmailAddress))
                throw new ArgumentException("OWA Email Address is required", nameof(configuration));
            
            if (string.IsNullOrWhiteSpace(_configuration.OwaPassword))
                throw new ArgumentException("OWA Password is required", nameof(configuration));
            
            if (string.IsNullOrWhiteSpace(_configuration.OwaServiceURI))
                throw new ArgumentException("OWA Service URI is required", nameof(configuration));

            // Initialize Exchange Service
            _exchangeService = new ExchangeService(ExchangeVersion.Exchange2013_SP1);
            _exchangeService.Credentials = new WebCredentials(_configuration.OwaEmailAddress, _configuration.OwaPassword);
            
            try
            {
                _exchangeService.Url = new Uri(_configuration.OwaServiceURI);
                _logger.LogInformation("OWA Service initialized for {EmailAddress} at {ServiceUri}", 
                    _configuration.OwaEmailAddress, _configuration.OwaServiceURI);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize OWA Service with URI: {ServiceUri}", _configuration.OwaServiceURI);
                throw new ArgumentException($"Invalid OWA Service URI: {_configuration.OwaServiceURI}", ex);
            }
        }

        /// <summary>
        /// Ensures the Exchange service connection is ready and available
        /// </summary>
        /// <returns>The configured ExchangeService instance</returns>
        /// <exception cref="InvalidOperationException">Thrown when service is disposed or connection failed</exception>
        private ExchangeService EnsureConnection()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(OwaService));

            if (_exchangeService == null)
                throw new InvalidOperationException("Exchange service connection is not available");

            return _exchangeService;
        }

        /// <summary>
        /// Retrieves emails from the OWA service
        /// </summary>
        /// <param name="request">Request containing email retrieval parameters</param>
        /// <returns>Response containing retrieved emails or error information</returns>
        public async System.Threading.Tasks.Task<GetEmailResponse> GetEmail(GetEmailRequest request)
        {
            var response = new GetEmailResponse
            {
                Emails = new List<Email>(),
                Success = false,
                Message = string.Empty,
                Service = EmailService.OWA
            };

            try
            {
                if (request == null)
                {
                    throw new ArgumentNullException(nameof(request), "GetEmailRequest cannot be null");
                }

                _logger.LogInformation("Starting email retrieval for {NumberOfEmails} emails", request.NumberOfEmails);

                // Define the folder to search (Inbox)
                FolderId inboxFolder = WellKnownFolderName.Inbox;

                // Create item view to retrieve oldest emails first
                ItemView view = new ItemView(request.NumberOfEmails);
                // {
                //     OrderBy = { { ItemSchema.DateTimeReceived, SortDirection.Ascending } }
                // };

                // Property set to load email details including body and attachments
                PropertySet propertySet = new PropertySet(BasePropertySet.FirstClassProperties)
                {
                    EmailMessageSchema.Body,
                    EmailMessageSchema.Attachments,
                    EmailMessageSchema.From,
                    EmailMessageSchema.ToRecipients,
                    EmailMessageSchema.CcRecipients,
                    EmailMessageSchema.BccRecipients
                };

                //view.PropertySet = propertySet;

                // Ensure connection is available
                var exchangeService = EnsureConnection();

                // Retrieve emails
                _logger.LogInformation("Retrieving emails from OWA service...");
                FindItemsResults<Item> findResults = exchangeService.FindItems(inboxFolder, view);

                _logger.LogInformation("Found {EmailCount} emails in inbox", findResults.Items.Count);

                // Process each email
                foreach (Item item in findResults.Items)
                {
                    if (item is EmailMessage emailMessage)
                    {
                        try
                        {
                            // Load additional properties if needed
                            await System.Threading.Tasks.Task.Run(() => emailMessage.Load(propertySet));

                            var email = MapToEmail(emailMessage);
                            response.Emails.Add(email);

                            _logger.LogDebug("Processed email: {Subject} from {From}", 
                                emailMessage.Subject, emailMessage.From?.ToString());
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing email: {Subject}", item.Subject);
                            // Continue processing other emails
                        }
                    }
                }

                response.Success = true;
                response.Count = response.Emails.Count;
                _logger.LogInformation("Successfully retrieved {EmailCount} emails", response.Emails.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving emails from OWA service");
                response.Success = false;
                response.Message = $"Failed to retrieve emails: {ex.Message}";
                
                // If it's an authentication failure, provide more specific information
                if (ex.Message.Contains("Unauthorized") || ex.Message.Contains("401"))
                {
                    response.Message = "Authentication failed. Please check OWA credentials.";
                }
            }

            return response;
        }

        /// <summary>
        /// Maps an Exchange EmailMessage to the unified Email entity
        /// </summary>
        /// <param name="emailMessage">Exchange email message</param>
        /// <returns>Mapped Email entity</returns>
        private Email MapToEmail(EmailMessage emailMessage)
        {
            var email = new Email
            {
                Id = emailMessage.Id?.UniqueId ?? Guid.NewGuid().ToString(),
                Service = EmailService.OWA,
                Subject = emailMessage.Subject ?? string.Empty,
                SentDateTime = emailMessage.DateTimeReceived != DateTime.MinValue ? emailMessage.DateTimeReceived : DateTime.UtcNow,
                Attachments = new List<EmailAttachment>()
            };

            // Map sender
            if (emailMessage.From != null)
            {
                email.From = emailMessage.From.Address ?? string.Empty;
            }

            // Map recipients
            email.To = MapEmailAddressList(emailMessage.ToRecipients);
            email.CC = MapEmailAddressList(emailMessage.CcRecipients);
            email.BCC = MapEmailAddressList(emailMessage.BccRecipients);

            // Map body - favor HTML over plain text
            if (emailMessage.Body != null)
            {
                if (emailMessage.Body.BodyType == BodyType.HTML)
                {
                    email.Body = emailMessage.Body.Text ?? string.Empty;
                }
                else if (emailMessage.Body.BodyType == BodyType.Text)
                {
                    email.Body = emailMessage.Body.Text ?? string.Empty;
                }
                else
                {
                    email.Body = emailMessage.Body.ToString() ?? string.Empty;
                }
            }

            // Map attachments (metadata only)
            if (emailMessage.Attachments != null && emailMessage.Attachments.Count > 0)
            {
                foreach (Attachment attachment in emailMessage.Attachments)
                {
                    if (attachment is FileAttachment fileAttachment)
                    {
                        var emailAttachment = new EmailAttachment
                        {
                            Name = fileAttachment.Name ?? "Unknown",
                            Size = fileAttachment.Size,
                            Type = GetFileExtension(fileAttachment.Name),
                            Content = null // Only retrieve metadata, not content
                        };

                        email.Attachments.Add(emailAttachment);

                        _logger.LogDebug("Mapped attachment: {Name} ({Size} bytes)", 
                            emailAttachment.Name, emailAttachment.Size);
                    }
                }
            }

            return email;
        }

        /// <summary>
        /// Maps Exchange email addresses to a list of strings
        /// </summary>
        /// <param name="recipients">Exchange email address collection</param>
        /// <returns>List of email addresses</returns>
        private List<string> MapEmailAddressList(EmailAddressCollection? recipients)
        {
            if (recipients == null || recipients.Count == 0)
                return new List<string>();

            var addresses = new List<string>();
            foreach (EmailAddress recipient in recipients)
            {
                if (!string.IsNullOrWhiteSpace(recipient.Address))
                {
                    addresses.Add(recipient.Address);
                }
            }

            return addresses;
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
        /// Releases all resources used by the OwaService
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the OwaService and optionally releases the managed resources
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // ExchangeService doesn't implement IDisposable, but we can clear credentials
                    if (_exchangeService != null)
                    {
                        _exchangeService.Credentials = null;
                        _logger.LogDebug("OWA Service disposed and credentials cleared");
                    }
                }

                _disposed = true;
            }
        }
    }
}
