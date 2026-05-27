using EmailAgent.Entities;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace EmailAgent.Services
{
    public class GmailService : IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly KeyVaultService _keyVaultService;
        private readonly ILogger<GmailService> _logger;
        private readonly string _emailAddress;
        private Google.Apis.Gmail.v1.GmailService? _gmailService;
        private DateTime _lastApiCall = DateTime.MinValue;
        private const int RATE_LIMIT_DELAY_MS = 100; // Minimum delay between API calls
        private bool _disposed = false;

        // Cache for OAuth configuration values
        private string? _googleClientId;
        private string? _googleClientSecret;

        public GmailService(IConfiguration configuration, KeyVaultService keyVaultService, ILogger<GmailService> logger, string emailAddress)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _keyVaultService = keyVaultService ?? throw new ArgumentNullException(nameof(keyVaultService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _emailAddress = emailAddress ?? throw new ArgumentNullException(nameof(emailAddress));

            _logger.LogInformation("Gmail Service initialized for: {EmailAddress}", _emailAddress);
        }

        /// <summary>
        /// Ensures the Gmail service connection is ready and available for the configured email address
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

        /// <summary>
        /// Converts an EmailFolder to the appropriate Gmail query string
        /// </summary>
        /// <param name="folder">The email folder to convert</param>
        /// <returns>Gmail query string appropriate for the folder</returns>
        /// <exception cref="ArgumentNullException">Thrown when folder is null</exception>
        /// <exception cref="ArgumentException">Thrown when folder type is not supported</exception>
        private string GetGmailQueryFromEmailFolder(EmailFolder folder)
        {
            if (folder == null)
                throw new ArgumentNullException(nameof(folder));

            // If ServiceSpecificId is provided, use it directly as the query
            if (!string.IsNullOrEmpty(folder.ServiceSpecificId))
            {
                return $"in:{folder.ServiceSpecificId}";
            }

            // Fall back to mapping based on FolderType
            return folder.FolderType switch
            {
                FolderType.Inbox => "in:inbox",
                FolderType.Sent => "in:sent",
                FolderType.Drafts => "in:draft",
                FolderType.Spam => "in:spam",
                FolderType.Trash => "in:trash",
                FolderType.Custom => string.IsNullOrEmpty(folder.ServiceSpecificId)
                    ? throw new ArgumentException($"Custom folder '{folder.FolderName}' requires ServiceSpecificId")
                    : $"in:{folder.ServiceSpecificId}",
                _ => throw new ArgumentException($"Unsupported folder type: {folder.FolderType}")
            };
        }

        public async Task<GetEmailResponse> GetEmail(GetEmailRequest request)
        {
            var response = new GetEmailResponse();

            try
            {
                _logger.LogInformation("Starting GetEmail request for {EmailAddress}: NumberOfEmails={NumberOfEmails}", _emailAddress, request.NumberOfEmails);

                // Get the effective folder (defaults to Inbox if no folder specified)
                var targetFolder = request.GetEffectiveFolder(EmailService.Gmail);
                _logger.LogDebug("Retrieving emails from folder: {FolderName} ({FolderType}) for {EmailAddress}",
                    targetFolder.FolderName, targetFolder.FolderType, _emailAddress);

                // Ensure Gmail service connection is available
                var gmailService = await EnsureConnection();

                // Get messages from the specified folder
                var messages = await GetFolderMessages(gmailService, targetFolder, request.NumberOfEmails);

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
            catch (Google.Apis.Auth.OAuth2.Responses.TokenResponseException ex) when (ex.Error?.Error == "invalid_grant")
            {
                _logger.LogError(ex, "Gmail OAuth token is invalid or revoked for {EmailAddress}. Clearing stored token.", _emailAddress);
                _gmailService = null;
                try { await _keyVaultService.DeleteGmailTokenAsync(_emailAddress); } catch { }
                response.Success = false;
                response.Message = "Gmail authorization has expired or been revoked. Please re-authorize the application.";
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

        /// <summary>
        /// Initializes the Gmail service with OAuth2 authentication and Key Vault token storage
        /// The first run will require interactive authentication, subsequent runs will use tokens from Key Vault
        /// Also handles migration from local file-based tokens if they exist
        /// </summary>
        private async Task InitializeGmailService()
        {
            try
            {
                _logger.LogInformation("Initializing Gmail service authentication for: {EmailAddress}", _emailAddress);

                // Load OAuth configuration from Key Vault
                await LoadOAuthConfiguration();

                // Create Key Vault-based token store
                var dataStore = new KeyVaultDataStore(_keyVaultService, _emailAddress, _logger);

                // Check for existing local tokens and migrate them if found
                await MigrateLocalTokensIfExists(dataStore);

                _logger.LogDebug("Using Key Vault token storage for email: {EmailAddress}", _emailAddress);

                // Use GoogleWebAuthorizationBroker with Key Vault storage
                var userCredential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    new ClientSecrets
                    {
                        ClientId = _googleClientId!,
                        ClientSecret = _googleClientSecret!
                    },
                    new[] { Google.Apis.Gmail.v1.GmailService.Scope.MailGoogleCom },
                    _emailAddress, // Using email address as user ID
                    CancellationToken.None,
                    dataStore); // This enables Key Vault token storage

                // Create Gmail service
                _gmailService = new Google.Apis.Gmail.v1.GmailService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = userCredential,
                    ApplicationName = "EmailAgent Gmail Service"
                });

                _logger.LogInformation("Gmail service initialized successfully for: {EmailAddress}", _emailAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Gmail service for: {EmailAddress}", _emailAddress);
                throw new InvalidOperationException($"Failed to authenticate with Gmail service for {_emailAddress}", ex);
            }
        }

        /// <summary>
        /// Loads OAuth configuration from Key Vault
        /// </summary>
        private async Task LoadOAuthConfiguration()
        {
            if (_googleClientId == null)
            {
                _googleClientId = await _keyVaultService.GetSecretAsync("googleClientId");
                if (string.IsNullOrWhiteSpace(_googleClientId))
                    throw new InvalidOperationException("googleClientId secret not found in Key Vault");
            }

            if (_googleClientSecret == null)
            {
                _googleClientSecret = await _keyVaultService.GetSecretAsync("googleClientSecret");
                if (string.IsNullOrWhiteSpace(_googleClientSecret))
                    throw new InvalidOperationException("googleClientSecret secret not found in Key Vault");
            }

            _logger.LogDebug("OAuth configuration loaded from Key Vault");
        }

        /// <summary>
        /// Migrates existing local tokens to Key Vault if they exist
        /// </summary>
        /// <param name="keyVaultDataStore">The Key Vault data store</param>
        private async Task MigrateLocalTokensIfExists(KeyVaultDataStore keyVaultDataStore)
        {
            try
            {
                var tokenStorePath = Path.Combine(Directory.GetCurrentDirectory(), "gmail_tokens");
                var localDataStore = new FileDataStore(tokenStorePath, true);

                // Check if token already exists in Key Vault
                var existingToken = await _keyVaultService.GetGmailTokenAsync(_emailAddress);
                if (!string.IsNullOrEmpty(existingToken))
                {
                    _logger.LogDebug("Token already exists in Key Vault for: {EmailAddress}", _emailAddress);
                    return;
                }

                // Check if local token exists
                var token = await localDataStore.GetAsync<Google.Apis.Auth.OAuth2.Responses.TokenResponse>(_emailAddress);
                if (token != null)
                {
                    _logger.LogInformation("Migrating local Gmail token to Key Vault for: {EmailAddress}", _emailAddress);

                    // Store in Key Vault
                    await keyVaultDataStore.StoreAsync(_emailAddress, token);

                    _logger.LogInformation("Successfully migrated Gmail token from local storage to Key Vault for: {EmailAddress}", _emailAddress);
                }
                else
                {
                    _logger.LogDebug("No local Gmail token found for migration for: {EmailAddress}", _emailAddress);
                }
            }
            catch (Exception ex)
            {
                // Log warning but don't fail the authentication process
                _logger.LogWarning(ex, "Failed to migrate local Gmail tokens for: {EmailAddress}. Will proceed with Key Vault storage.", _emailAddress);
            }
        }

        private async Task<List<Message>> GetFolderMessages(Google.Apis.Gmail.v1.GmailService gmailService, EmailFolder folder, int numberOfEmails)
        {
            _logger.LogInformation("Getting messages from folder {FolderName}: NumberOfEmails={NumberOfEmails}",
                folder.FolderName, numberOfEmails);

            var messages = new List<Message>();

            // Get Gmail query string for the folder
            var queryString = GetGmailQueryFromEmailFolder(folder);
            _logger.LogDebug("Using Gmail query: {Query}", queryString);

            // Get message IDs from the specified folder
            var request = gmailService.Users.Messages.List("me");
            request.Q = queryString;
            request.MaxResults = numberOfEmails;

            await RateLimitDelay();
            var response = await request.ExecuteAsync();

            if (response.Messages == null || response.Messages.Count == 0)
            {
                _logger.LogInformation("No messages found in folder {FolderName}", folder.FolderName);
                return messages;
            }

            _logger.LogInformation("Found {MessageCount} messages in folder {FolderName}", response.Messages.Count, folder.FolderName);

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

        public async Task<CreateFolderResponse> CreateFolder(string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                throw new ArgumentException("Folder name cannot be empty", nameof(folderName));

            try
            {
                _logger.LogInformation("Creating Gmail label {FolderName} for {EmailAddress}", folderName, _emailAddress);

                var gmailService = await EnsureConnection();
                var label = new Label { Name = folderName };

                await RateLimitDelay();
                var created = await gmailService.Users.Labels.Create(label, "me").ExecuteAsync();

                _logger.LogInformation("Successfully created Gmail label {LabelName} with ID {LabelId}", created.Name, created.Id);
                return new CreateFolderResponse
                {
                    Success = true,
                    FolderName = created.Name ?? folderName,
                    ServiceSpecificId = created.Id,
                    Service = EmailService.Gmail
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Gmail label {FolderName}", folderName);
                return new CreateFolderResponse { Success = false, Message = ex.Message, Service = EmailService.Gmail };
            }
        }

        public async Task<bool> MoveEmailToFolder(string emailId, string destinationFolder)
        {
            if (string.IsNullOrWhiteSpace(emailId))
                throw new ArgumentException("Email ID cannot be empty", nameof(emailId));
            if (string.IsNullOrWhiteSpace(destinationFolder))
                throw new ArgumentException("Destination folder cannot be empty", nameof(destinationFolder));

            try
            {
                _logger.LogInformation("Moving email {EmailId} to folder {Folder} for {EmailAddress}", emailId, destinationFolder, _emailAddress);

                var gmailService = await EnsureConnection();

                // Find the label by ID or name
                await RateLimitDelay();
                var labelsResponse = await gmailService.Users.Labels.List("me").ExecuteAsync();

                var targetLabel = labelsResponse.Labels?.FirstOrDefault(l =>
                    l.Id == destinationFolder ||
                    string.Equals(l.Name, destinationFolder, StringComparison.OrdinalIgnoreCase));

                if (targetLabel == null)
                {
                    _logger.LogWarning("Gmail label {Folder} not found for {EmailAddress}", destinationFolder, _emailAddress);
                    return false;
                }

                var modifyRequest = new ModifyMessageRequest
                {
                    AddLabelIds = new List<string> { targetLabel.Id },
                    RemoveLabelIds = new List<string> { "INBOX" }
                };

                await RateLimitDelay();
                await gmailService.Users.Messages.Modify(modifyRequest, "me", emailId).ExecuteAsync();

                _logger.LogInformation("Successfully moved email {EmailId} to label {LabelName}", emailId, targetLabel.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving email {EmailId} to folder {Folder}", emailId, destinationFolder);
                return false;
            }
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
