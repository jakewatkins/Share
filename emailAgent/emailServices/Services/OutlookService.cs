using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Extensions.Logging;
using EmailAgent.Core;
using EmailAgent.Entities;

namespace EmailAgent.Services
{
    /// <summary>
    /// Custom authentication provider that integrates MSAL with Microsoft Graph v5
    /// </summary>
    public class MSALAuthenticationProvider : IAuthenticationProvider
    {
        private readonly IPublicClientApplication _clientApp;
        private readonly string[] _scopes;
        private readonly ILogger _logger;

        public MSALAuthenticationProvider(IPublicClientApplication clientApp, string[] scopes, ILogger logger)
        {
            _clientApp = clientApp ?? throw new ArgumentNullException(nameof(clientApp));
            _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task AuthenticateRequestAsync(RequestInformation request, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
        {
            var token = await GetAccessTokenAsync();
            request.Headers.Add("Authorization", $"Bearer {token}");
        }

        private async Task<string> GetAccessTokenAsync()
        {
            try
            {
                // Try to get token silently first
                var accounts = await _clientApp.GetAccountsAsync();
                if (accounts.Any())
                {
                    _logger.LogDebug("Attempting silent token acquisition for account: {Account}", accounts.FirstOrDefault()?.Username);
                    var result = await _clientApp.AcquireTokenSilent(_scopes, accounts.FirstOrDefault())
                        .ExecuteAsync();
                    _logger.LogDebug("Successfully acquired token silently");
                    return result.AccessToken;
                }
            }
            catch (MsalUiRequiredException ex)
            {
                _logger.LogDebug(ex, "Silent token acquisition failed, requiring interactive authentication");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during silent token acquisition");
            }

            // Acquire token interactively
            _logger.LogInformation("Performing interactive authentication");
            var interactiveResult = await _clientApp.AcquireTokenInteractive(_scopes)
                .WithPrompt(Microsoft.Identity.Client.Prompt.SelectAccount)
                .ExecuteAsync();

            _logger.LogInformation("Interactive authentication successful for account: {Account}", interactiveResult.Account?.Username);
            return interactiveResult.AccessToken;
        }
    }

    /// <summary>
    /// Service for retrieving emails from Microsoft Outlook using Microsoft Graph API
    /// </summary>
    public class OutlookService : IDisposable
    {
        private readonly AgentConfiguration _configuration;
        private readonly KeyVaultService _keyVaultService;
        private readonly ILogger _logger;
        private readonly string _emailAddress;
        private GraphServiceClient _graphClient = null!; // Initialized in constructor via InitializeGraphClient
        private readonly string[] _scopes = { "Mail.Read", "Mail.ReadWrite" };
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the OutlookService
        /// </summary>
        /// <param name="configuration">Agent configuration containing Outlook settings</param>
        /// <param name="keyVaultService">Service for managing OAuth tokens in Azure Key Vault</param>
        /// <param name="logger">Logger for diagnostic information</param>
        /// <param name="emailAddress">Email address for this Outlook service instance</param>
        /// <exception cref="ArgumentNullException">Thrown when configuration, keyVaultService, logger, or emailAddress is null</exception>
        /// <exception cref="ArgumentException">Thrown when required Outlook configuration values are missing</exception>
        public OutlookService(AgentConfiguration configuration, KeyVaultService keyVaultService, ILogger logger, string emailAddress)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _keyVaultService = keyVaultService ?? throw new ArgumentNullException(nameof(keyVaultService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _emailAddress = emailAddress ?? throw new ArgumentNullException(nameof(emailAddress));

            // Validate required configuration values
            if (string.IsNullOrWhiteSpace(_configuration.OutlookClientId))
                throw new ArgumentException("Outlook Client ID is required", nameof(configuration));

            if (string.IsNullOrWhiteSpace(_configuration.OutlookSecret))
                throw new ArgumentException("Outlook Secret is required", nameof(configuration));

            // Note: Graph client initialization is deferred to first use due to async requirements
            _logger.LogInformation("Outlook Service initialized with Client ID: {ClientId} for email: {EmailAddress}",
                _configuration.OutlookClientId, _emailAddress);
        }

        /// <summary>
        /// Ensures the Graph service client connection is ready and available
        /// </summary>
        /// <returns>The configured GraphServiceClient instance</returns>
        /// <exception cref="InvalidOperationException">Thrown when service is disposed or connection failed</exception>
        private async Task<GraphServiceClient> EnsureConnectionAsync()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(OutlookService));

            if (_graphClient == null)
                await InitializeGraphClientAsync();

            if (_graphClient == null)
                throw new InvalidOperationException("Graph service client connection is not available");

            return _graphClient;
        }

        /// <summary>
        /// Gets the appropriate Microsoft Graph folder path for an EmailFolder
        /// </summary>
        /// <param name="folder">The email folder to convert</param>
        /// <returns>The folder path string for Microsoft Graph API</returns>
        /// <exception cref="ArgumentNullException">Thrown when folder is null</exception>
        /// <exception cref="ArgumentException">Thrown when folder type is not supported</exception>
        private string GetGraphFolderPath(EmailFolder folder)
        {
            if (folder == null)
                throw new ArgumentNullException(nameof(folder));

            // If ServiceSpecificId is provided, use it directly as the folder path
            if (!string.IsNullOrEmpty(folder.ServiceSpecificId))
            {
                return folder.ServiceSpecificId;
            }

            // Fall back to mapping based on FolderType
            return folder.FolderType switch
            {
                FolderType.Inbox => "Inbox",
                FolderType.Sent => "SentItems",
                FolderType.Drafts => "Drafts",
                FolderType.Spam => "JunkEmail",
                FolderType.Trash => "DeletedItems",
                FolderType.Custom => string.IsNullOrEmpty(folder.ServiceSpecificId)
                    ? throw new ArgumentException($"Custom folder '{folder.FolderName}' requires ServiceSpecificId")
                    : folder.ServiceSpecificId,
                _ => throw new ArgumentException($"Unsupported folder type: {folder.FolderType}")
            };
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

                // Get the effective folder (defaults to Inbox if no folder specified)
                var targetFolder = request.GetEffectiveFolder(EmailService.Outlook);
                _logger.LogDebug("Retrieving emails from folder: {FolderName} ({FolderType})",
                    targetFolder.FolderName, targetFolder.FolderType);

                // Ensure Graph client connection is available
                var graphClient = await EnsureConnectionAsync();

                // Get the folder path for Microsoft Graph
                var folderPath = GetGraphFolderPath(targetFolder);

                // Get emails from the specified folder, ordered by ReceivedDateTime (oldest first)
                var messages = await graphClient.Me.MailFolders[folderPath].Messages
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Orderby = new string[] { "receivedDateTime asc" };
                        requestConfiguration.QueryParameters.Skip = request.StartIndex;
                        requestConfiguration.QueryParameters.Top = request.NumberOfEmails;
                        requestConfiguration.QueryParameters.Expand = new string[] { "attachments" };
                    });
                _logger.LogInformation("Found {EmailCount} emails in folder {FolderName}", messages?.Value?.Count ?? 0, targetFolder.FolderName);

                int processedCount = 0;

                // Process each email
                if (messages?.Value != null)
                {
                    foreach (var message in messages.Value)
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
                var graphClient = await EnsureConnectionAsync();

                _logger.LogInformation("Attempting to delete email with ID: {EmailId}", email.Id);

                // Use the Email entity's id value to call the Microsoft Graph API's Messages.Delete method
                await graphClient.Me.Messages[email.Id]
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
        /// Initializes the Microsoft Graph client with authentication and persistent token storage
        /// The first run will require interactive authentication, subsequent runs will use cached tokens
        /// </summary>
        private async Task InitializeGraphClientAsync()
        {
            _logger.LogDebug("Initializing GraphServiceClient with Client ID: {ClientId} for email: {EmailAddress}",
                _configuration.OutlookClientId, _emailAddress);

            try
            {
                // Create the public client application for delegated permissions
                // Using "consumers" authority to support personal Microsoft accounts
                var app = PublicClientApplicationBuilder
                    .Create(_configuration.OutlookClientId)
                    .WithAuthority("https://login.microsoftonline.com/consumers")
                    .WithRedirectUri("http://localhost")
                    .Build();

                // Set up Key Vault-based token cache
                var keyVaultTokenCache = new KeyVaultTokenCache(_keyVaultService, _emailAddress, _logger);

                // Register Key Vault token cache callbacks with MSAL
                app.UserTokenCache.SetBeforeAccess(keyVaultTokenCache.BeforeAccessNotification);
                app.UserTokenCache.SetAfterAccess(keyVaultTokenCache.AfterAccessNotification);

                _logger.LogDebug("Created PublicClientApplication with Azure Key Vault token cache for: {EmailAddress}", _emailAddress);

                // Create a custom authentication provider that works with Microsoft Graph v5
                var authProvider = new MSALAuthenticationProvider(app, _scopes, _logger);

                _graphClient = new GraphServiceClient(authProvider);

                _logger.LogInformation("Initialized GraphServiceClient with Azure Key Vault token storage for email: {EmailAddress}", _emailAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize GraphServiceClient with Azure Key Vault token storage. Falling back to in-memory cache for email: {EmailAddress}", _emailAddress);

                // Fallback to in-memory cache if Key Vault storage fails
                var app = PublicClientApplicationBuilder
                    .Create(_configuration.OutlookClientId)
                    .WithAuthority("https://login.microsoftonline.com/consumers")
                    .WithRedirectUri("http://localhost")
                    .Build();

                var authProvider = new MSALAuthenticationProvider(app, _scopes, _logger);
                _graphClient = new GraphServiceClient(authProvider);

                _logger.LogWarning("Using in-memory token cache for {EmailAddress}. User may need to re-authenticate on each run.", _emailAddress);
            }
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
