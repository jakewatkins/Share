using EmailAgent.Core;
using EmailAgent.Entities;
using EmailAgent.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace emailServices.Tests
{
    public class OutlookDeleteIntegrationTests : IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly OutlookService _outlookService;

        public OutlookDeleteIntegrationTests()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("settings.json", optional: false, reloadOnChange: true)
                .Build();

            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            var keyVaultLogger = _loggerFactory.CreateLogger<KeyVaultService>();
            var keyVaultService = new KeyVaultService(configuration, keyVaultLogger);

            var agentConfig = new AgentConfiguration(configuration);
            var logger = _loggerFactory.CreateLogger<OutlookService>();
            _outlookService = new OutlookService(agentConfig, keyVaultService, logger, "RunsInCirclesScreaming@msn.com");
        }

        [Fact]
        public async Task FetchAndDelete_InboxEmail_ShouldSucceed()
        {
            // Fetch one email from inbox
            var request = new GetEmailRequest
            {
                NumberOfEmails = 1,
                Folder = EmailFolder.CreateInboxFolder(EmailService.Outlook)
            };

            var response = await _outlookService.GetEmail(request);

            Assert.True(response.Success, $"GetEmail failed: {response.Message}");
            Assert.NotNull(response.Emails);
            Assert.True(response.Emails.Count > 0, "No emails found to delete");

            var email = response.Emails[0];
            Console.WriteLine($"Deleting - Date: {email.SentDateTime:yyyy-MM-dd HH:mm:ss} | From: {email.From} | Subject: {email.Subject}");

            // Delete the email
            var deleted = await _outlookService.DeleteEmail(email);

            Assert.True(deleted, "DeleteEmail should return true on success");
        }

        public void Dispose()
        {
            _loggerFactory?.Dispose();
        }
    }
}
