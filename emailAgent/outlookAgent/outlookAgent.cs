using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using Serilog;
using System.Text;

namespace emailAgent
{
    public class outlookAgent
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly GraphServiceClient _graphClient;
        private readonly TokenManager _tokenManager;
        private readonly int _retrievalCount;
        private readonly long _maxAttachmentSize;
        private readonly string[] _scopes = { "Mail.Read", "Mail.ReadWrite" };

        public outlookAgent(IConfiguration configuration)
        {
            _configuration = configuration;
            
            // Initialize Serilog
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(_configuration)
                .CreateLogger();
            
            _logger = Log.ForContext<outlookAgent>();

            // Get configuration values
            _retrievalCount = _configuration.GetValue<int>("RetrievalCount", 50);
            _maxAttachmentSize = _configuration.GetValue<long>("MaxAttachmentSize", 10485760);

            _logger.Information("OutlookAgent initialized with RetrievalCount: {RetrievalCount}, MaxAttachmentSize: {MaxAttachmentSize}", 
                _retrievalCount, _maxAttachmentSize);

            // Initialize Microsoft Graph
            var clientId = Environment.GetEnvironmentVariable("valetClientId");
            var clientSecret = Environment.GetEnvironmentVariable("valetSecret");

            if (string.IsNullOrEmpty(clientId))
            {
                _logger.Error("valetClientId environment variable not found");
                throw new InvalidOperationException("valetClientId environment variable is required");
            }

            // Create the public client application for delegated permissions
            var app = PublicClientApplicationBuilder
                .Create(clientId)
                .WithAuthority("https://login.microsoftonline.com/common")
                .WithRedirectUri("http://localhost")
                .Build();

            _tokenManager = new TokenManager(app);

            // Create Graph client with InteractiveAuthenticationProvider
            var authProvider = new InteractiveAuthenticationProvider(app, _scopes);
            _graphClient = new GraphServiceClient(authProvider);
        }

        public async Task<GetEmailResponse> GetEmail(int startIndex, int numberOfEmails)
        {
            _logger.Information("GetEmail called with startIndex: {StartIndex}, numberOfEmails: {NumberOfEmails}", 
                startIndex, numberOfEmails);

            try
            {
                // Get emails from inbox, ordered by ReceivedDateTime (oldest first)
                var messages = await _graphClient.Me.MailFolders.Inbox.Messages
                    .Request()
                    .OrderBy("receivedDateTime asc")
                    .Skip(startIndex)
                    .Top(numberOfEmails)
                    .Expand("attachments")
                    .GetAsync();

                var emails = new List<Email>();
                int processedCount = 0;

                foreach (var message in messages)
                {
                    try
                    {
                        var email = await ConvertToEmailAsync(message);
                        emails.Add(email);
                        processedCount++;
                        
                        _logger.Debug("Processed email: {Subject} from {From}", 
                            email.Subject, email.From);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning(ex, "Failed to process email with ID: {MessageId}", message.Id);
                        // Continue processing other emails
                    }
                }

                string responseMessage = processedCount < numberOfEmails ? "Last" : "ok";
                
                _logger.Information("GetEmail completed. Requested: {Requested}, Retrieved: {Retrieved}, Message: {Message}", 
                    numberOfEmails, processedCount, responseMessage);

                return new GetEmailResponse
                {
                    Success = true,
                    Message = responseMessage,
                    Count = processedCount,
                    Emails = emails
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to retrieve emails");
                return new GetEmailResponse
                {
                    Success = false,
                    Message = $"Failed to retrieve emails: {ex.Message}",
                    Count = 0,
                    Emails = new List<Email>()
                };
            }
        }

        private Task<Email> ConvertToEmailAsync(Message message)
        {
            var email = new Email
            {
                From = message.From?.EmailAddress?.Address ?? string.Empty,
                To = message.ToRecipients?.Select(r => r.EmailAddress?.Address ?? string.Empty).ToList() ?? new List<string>(),
                SentDateTime = message.SentDateTime?.DateTime ?? DateTime.MinValue,
                Subject = message.Subject ?? string.Empty,
                MessageBody = message.Body?.Content ?? string.Empty,
                Attachments = new List<EmailAttachment>()
            };

            // Process attachments
            if (message.Attachments?.Any() == true)
            {
                foreach (var attachment in message.Attachments)
                {
                    try
                    {
                        var emailAttachment = new EmailAttachment
                        {
                            Name = attachment.Name ?? string.Empty,
                            Type = attachment.ContentType ?? string.Empty,
                            Size = attachment.Size ?? 0
                        };

                        // Only download content if attachment is smaller than max size
                        if (emailAttachment.Size <= _maxAttachmentSize && attachment is FileAttachment fileAttachment)
                        {
                            if (fileAttachment.ContentBytes != null)
                            {
                                emailAttachment.Content = Convert.ToBase64String(fileAttachment.ContentBytes);
                                _logger.Debug("Downloaded attachment: {Name} ({Size} bytes)", 
                                    emailAttachment.Name, emailAttachment.Size);
                            }
                        }
                        else
                        {
                            _logger.Debug("Skipped large attachment: {Name} ({Size} bytes, limit: {Limit} bytes)", 
                                emailAttachment.Name, emailAttachment.Size, _maxAttachmentSize);
                        }

                        email.Attachments.Add(emailAttachment);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning(ex, "Failed to process attachment: {AttachmentName}", attachment.Name);
                        // Continue processing other attachments
                    }
                }
            }

            return Task.FromResult(email);
        }
    }
}
