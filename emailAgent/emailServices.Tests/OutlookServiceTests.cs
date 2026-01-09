using EmailAgent.Core;
using EmailAgent.Entities;
using EmailAgent.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace emailServices.Tests
{
    public class OutlookServiceTests : IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly ILoggerFactory _loggerFactory;
        private readonly OutlookService _outlookService;

        public OutlookServiceTests()
        {
            // Build configuration from settings.json
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("settings.json", optional: false, reloadOnChange: true)
                .Build();

            // Create logger factory
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Initialize OutlookService
            var agentConfig = new AgentConfiguration(_configuration);
            var logger = _loggerFactory.CreateLogger<OutlookService>();
            _outlookService = new OutlookService(agentConfig, logger);
        }

        [Fact]
        public async Task GetEmail_FromRecruitersFolder_ShouldReturnEmails()
        {
            // Arrange
            var recruitersFolder = new EmailFolder(
                "Recruiters",
                FolderType.Custom,
                EmailService.Outlook,
                "Recruiters"  // Service-specific folder name
            );

            var request = new GetEmailRequest
            {
                NumberOfEmails = 10,
                Folder = recruitersFolder
            };

            // Act
            var response = await _outlookService.GetEmail(request);

            // Assert
            Assert.True(response.Success, $"GetEmail should succeed. Message: {response.Message}");
            Assert.NotNull(response.Emails);
            Assert.True(response.Emails.Count > 0, "Should return at least one email from Recruiters folder");
            
            // Verify all emails are from the Outlook service
            Assert.All(response.Emails, email => 
                Assert.Equal(EmailService.Outlook, email.Service));

            // Log results for inspection
            foreach (var email in response.Emails)
            {
                Console.WriteLine($"{email.SentDateTime:yyyy-MM-dd HH:mm:ss} - From: {email.From} - Subject: {email.Subject}");
            }
        }

        [Fact]
        public async Task GetEmail_FromRecruitersFolder_WithLimit_ShouldRespectLimit()
        {
            // Arrange
            const int requestedCount = 5;
            var recruitersFolder = new EmailFolder(
                "Recruiters",
                FolderType.Custom,
                EmailService.Outlook,
                "Recruiters"
            );

            var request = new GetEmailRequest
            {
                NumberOfEmails = requestedCount,
                Folder = recruitersFolder
            };

            // Act
            var response = await _outlookService.GetEmail(request);

            // Assert
            Assert.True(response.Success, $"GetEmail should succeed. Message: {response.Message}");
            Assert.NotNull(response.Emails);
            Assert.True(response.Emails.Count <= requestedCount, 
                $"Should return at most {requestedCount} emails, but got {response.Emails.Count}");
        }

        [Fact]
        public async Task GetEmail_FromRecruitersFolder_ShouldHaveRequiredProperties()
        {
            // Arrange
            var recruitersFolder = new EmailFolder(
                "Recruiters",
                FolderType.Custom,
                EmailService.Outlook,
                "Recruiters"
            );

            var request = new GetEmailRequest
            {
                NumberOfEmails = 1,
                Folder = recruitersFolder
            };

            // Act
            var response = await _outlookService.GetEmail(request);

            // Assert
            Assert.True(response.Success);
            Assert.NotNull(response.Emails);
            
            if (response.Emails.Count > 0)
            {
                var email = response.Emails[0];
                
                // Verify essential properties are populated
                Assert.NotNull(email.Subject);
                Assert.NotNull(email.From);
                Assert.NotEqual(default(DateTime), email.SentDateTime);
                Assert.Equal(EmailService.Outlook, email.Service);
                
                // Body might be null or empty, but should be a string
                Assert.NotNull(email.Body);
            }
        }

        public void Dispose()
        {
            _loggerFactory?.Dispose();
        }
    }
}
