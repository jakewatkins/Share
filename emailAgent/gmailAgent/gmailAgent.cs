using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Globalization;
using System.Text;

namespace emailAgent;

public class gmailAgent
{
    private readonly IConfiguration _configuration;
    private readonly ILogger _logger;
    private readonly int _retrievalCount;
    private readonly long _maxAttachmentSize;
    private readonly string _keyvaultName;
    private GmailService? _gmailService;
    private DateTime _lastApiCall = DateTime.MinValue;

    public gmailAgent(IConfiguration configuration)
    {
        _configuration = configuration;
        _logger = Log.ForContext<gmailAgent>();
        
        // Get configuration values with defaults
        _retrievalCount = _configuration.GetValue<int>("RetrievalCount", 500);
        _maxAttachmentSize = _configuration.GetValue<long>("MaxAttachmentSize", 1048576); // 1MB default
        _keyvaultName = _configuration.GetValue<string>("keyvaultName") ?? throw new ArgumentException("keyvaultName is required in configuration");
        
        _logger.Information("Gmail Agent initialized with RetrievalCount: {RetrievalCount}, MaxAttachmentSize: {MaxAttachmentSize}", 
            _retrievalCount, _maxAttachmentSize);
    }

    public async Task<GetEmailResult> GetEmail(int startIndex, int numberOfEmails)
    {
        try
        {
            _logger.Information("Starting GetEmail request: StartIndex={StartIndex}, NumberOfEmails={NumberOfEmails}", 
                startIndex, numberOfEmails);

            // Initialize Gmail service if not already done
            if (_gmailService == null)
            {
                await InitializeGmailService();
            }

            // Get inbox messages
            var messages = await GetInboxMessages(startIndex, numberOfEmails);
            
            if (messages.Count == 0)
            {
                _logger.Information("No messages found for startIndex={StartIndex}", startIndex);
                return new GetEmailResult
                {
                    Success = true,
                    Message = startIndex == 0 ? "No emails found" : "Last",
                    Count = 0,
                    Emails = new List<Email>()
                };
            }

            // Convert messages to Email objects
            var emails = new List<Email>();
            foreach (var message in messages)
            {
                await RateLimitDelay();
                var email = await ConvertMessageToEmail(message);
                if (email != null)
                {
                    emails.Add(email);
                }
            }

            // Sort by sent date (oldest first)
            emails = emails.OrderBy(e => e.SentDateTime).ToList();

            var result = new GetEmailResult
            {
                Success = true,
                Message = emails.Count < numberOfEmails ? "Last" : "ok",
                Count = emails.Count,
                Emails = emails
            };

            _logger.Information("GetEmail completed successfully: Retrieved {Count} emails", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in GetEmail: StartIndex={StartIndex}, NumberOfEmails={NumberOfEmails}", 
                startIndex, numberOfEmails);
            
            string errorMessage = ex.Message;
            if (ex.InnerException != null)
            {
                errorMessage += $" Inner Exception: {ex.InnerException.Message}";
            }

            return new GetEmailResult
            {
                Success = false,
                Message = errorMessage,
                Count = 0,
                Emails = new List<Email>()
            };
        }
    }

    private async Task InitializeGmailService()
    {
        _logger.Information("Initializing Gmail service");
        
        // Get credentials from Azure Key Vault
        var credential = new DefaultAzureCredential();
        var client = new SecretClient(new Uri($"https://{_keyvaultName}.vault.azure.net/"), credential);
        
        var clientId = await client.GetSecretAsync("googleClientId");
        var clientSecret = await client.GetSecretAsync("googleClientSecret");
        var mailboxId = await client.GetSecretAsync("googleCalendarId");
        
        _logger.Information("Retrieved secrets from Key Vault: {KeyVaultName}", _keyvaultName);

        // Create OAuth2 credentials
        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = clientId.Value.Value,
                ClientSecret = clientSecret.Value.Value
            },
            Scopes = new[] { GmailService.Scope.GmailModify }
        });

        // For service account or pre-authorized scenarios, we'll use the mailbox ID as the user
        var userCredential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            new ClientSecrets
            {
                ClientId = clientId.Value.Value,
                ClientSecret = clientSecret.Value.Value
            },
            new[] { GmailService.Scope.GmailModify },
            mailboxId.Value.Value,
            CancellationToken.None);

        // Create Gmail service
        _gmailService = new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = userCredential,
            ApplicationName = "Gmail Agent"
        });

        _logger.Information("Gmail service initialized successfully");
    }

    private async Task<List<Message>> GetInboxMessages(int startIndex, int numberOfEmails)
    {
        _logger.Information("Getting inbox messages: StartIndex={StartIndex}, NumberOfEmails={NumberOfEmails}", 
            startIndex, numberOfEmails);

        var messages = new List<Message>();
        
        // Get message IDs from inbox
        var request = _gmailService!.Users.Messages.List("me");
        request.Q = "in:inbox";
        request.MaxResults = numberOfEmails + startIndex; // Get more than needed to handle pagination
        
        await RateLimitDelay();
        var response = await request.ExecuteAsync();
        
        if (response.Messages == null || response.Messages.Count == 0)
        {
            _logger.Information("No messages found in inbox");
            return messages;
        }

        // Get the messages we need based on start index
        var messageIds = response.Messages.Skip(startIndex).Take(numberOfEmails).ToList();
        
        _logger.Information("Found {TotalMessages} messages in inbox, retrieving {RequestedCount} starting from index {StartIndex}", 
            response.Messages.Count, messageIds.Count, startIndex);

        // Get full message details for each message
        foreach (var messageId in messageIds)
        {
            await RateLimitDelay();
            var messageRequest = _gmailService.Users.Messages.Get("me", messageId.Id);
            messageRequest.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;
            
            var message = await messageRequest.ExecuteAsync();
            messages.Add(message);
        }

        return messages;
    }

    private async Task<Email?> ConvertMessageToEmail(Message message)
    {
        try
        {
            var email = new Email
            {
                EmailMessageID = message.Id ?? string.Empty
            };

            // Get internal date
            if (message.InternalDate.HasValue)
            {
                email.SentDateTime = DateTimeOffset.FromUnixTimeMilliseconds(message.InternalDate.Value).DateTime;
            }

            // Parse headers
            if (message.Payload?.Headers != null)
            {
                foreach (var header in message.Payload.Headers)
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
                            // If internal date is not available, try to parse the date header
                            if (email.SentDateTime == default && DateTime.TryParse(header.Value, out var parsedDate))
                            {
                                email.SentDateTime = parsedDate;
                            }
                            break;
                    }
                }
            }

            // Parse message body and attachments
            if (message.Payload != null)
            {
                await ParseMessagePart(message.Payload, email, message.Id ?? string.Empty);
            }

            _logger.Debug("Converted message {MessageId} to Email object", message.Id);
            return email;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error converting message {MessageId} to Email object", message.Id);
            return null;
        }
    }

    private async Task ParseMessagePart(MessagePart part, Email email, string messageId)
    {
        if (part.Parts != null && part.Parts.Count > 0)
        {
            // Multipart message
            foreach (var subPart in part.Parts)
            {
                await ParseMessagePart(subPart, email, messageId);
            }
        }
        else
        {
            // Single part
            var mimeType = part.MimeType?.ToLowerInvariant();
            
            if (mimeType == "text/plain")
            {
                email.PlainMessageBody = GetMessagePartContent(part);
            }
            else if (mimeType == "text/html")
            {
                email.HtmlMessageBody = GetMessagePartContent(part);
            }
            else if (!string.IsNullOrEmpty(part.Filename))
            {
                // Attachment
                var attachment = await CreateAttachment(part, messageId);
                if (attachment != null)
                {
                    email.Attachments.Add(attachment);
                }
            }
        }
    }

    private string GetMessagePartContent(MessagePart part)
    {
        if (part.Body?.Data != null)
        {
            var data = Convert.FromBase64String(part.Body.Data.Replace('-', '+').Replace('_', '/'));
            return Encoding.UTF8.GetString(data);
        }
        return string.Empty;
    }

    private async Task<EmailAttachment?> CreateAttachment(MessagePart part, string messageId)
    {
        try
        {
            var attachment = new EmailAttachment
            {
                Name = part.Filename ?? "unknown",
                Type = part.MimeType ?? "application/octet-stream",
                Size = part.Body?.Size ?? 0
            };

            if (attachment.Size <= _maxAttachmentSize)
            {
                // Download attachment content
                if (part.Body?.AttachmentId != null)
                {
                    await RateLimitDelay();
                    var attachmentRequest = _gmailService!.Users.Messages.Attachments.Get("me", messageId, part.Body.AttachmentId);
                    var attachmentData = await attachmentRequest.ExecuteAsync();
                    
                    if (attachmentData.Data != null)
                    {
                        attachment.Content = attachmentData.Data.Replace('-', '+').Replace('_', '/');
                    }
                }
                else if (part.Body?.Data != null)
                {
                    attachment.Content = part.Body.Data.Replace('-', '+').Replace('_', '/');
                }
            }
            else
            {
                // Attachment too large, set content to null
                attachment.Content = null;
                _logger.Information("Attachment {AttachmentName} size {Size} exceeds maximum {MaxSize}, content not downloaded", 
                    attachment.Name, attachment.Size, _maxAttachmentSize);
            }

            return attachment;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error creating attachment for part {PartId}", part.PartId);
            return null;
        }
    }

    private List<string> ParseEmailAddresses(string addressString)
    {
        if (string.IsNullOrWhiteSpace(addressString))
        {
            return new List<string>();
        }

        // Simple email parsing - split by comma and trim
        return addressString.Split(',')
            .Select(addr => addr.Trim())
            .Where(addr => !string.IsNullOrWhiteSpace(addr))
            .ToList();
    }

    private async Task RateLimitDelay()
    {
        var timeSinceLastCall = DateTime.UtcNow - _lastApiCall;
        if (timeSinceLastCall.TotalSeconds < 1)
        {
            var delayMs = (int)(1000 - timeSinceLastCall.TotalMilliseconds);
            _logger.Debug("Rate limiting: Delaying {DelayMs}ms", delayMs);
            await Task.Delay(delayMs);
        }
        _lastApiCall = DateTime.UtcNow;
    }
}
