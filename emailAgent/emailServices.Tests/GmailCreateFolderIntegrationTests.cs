using EmailAgent.Entities;
using EmailAgent.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace emailServices.Tests
{
    public class GmailCreateFolderIntegrationTests : IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly GmailService _gmailService;

        public GmailCreateFolderIntegrationTests()
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

            var logger = _loggerFactory.CreateLogger<GmailService>();
            _gmailService = new GmailService(configuration, keyVaultService, logger, "jakew@guerillaprogrammer.com");
        }

        [Fact]
        public async Task CreateFolder_WithValidName_ShouldReturnSuccessWithId()
        {
            var folderName = $"TestFolder-{DateTime.UtcNow:yyyyMMddHHmmss}";

            var response = await _gmailService.CreateFolder(folderName);

            Assert.True(response.Success, $"CreateFolder failed: {response.Message}");
            Assert.Equal(EmailService.Gmail, response.Service);
            Assert.Equal(folderName, response.FolderName);
            Assert.NotNull(response.ServiceSpecificId);
            Assert.NotEmpty(response.ServiceSpecificId);

            Console.WriteLine($"Created Gmail label: {response.FolderName} (ID: {response.ServiceSpecificId})");
        }

        [Fact]
        public async Task MoveEmailToFolder_ShouldSucceed()
        {
            // Create a test label to move into
            var folderName = $"MoveTest-{DateTime.UtcNow:yyyyMMddHHmmss}";
            var createResponse = await _gmailService.CreateFolder(folderName);
            Assert.True(createResponse.Success, $"CreateFolder failed: {createResponse.Message}");

            // Fetch one email from inbox
            var getRequest = new GetEmailRequest
            {
                NumberOfEmails = 1,
                Folder = EmailFolder.CreateInboxFolder(EmailService.Gmail)
            };
            var getResponse = await _gmailService.GetEmail(getRequest);
            Assert.True(getResponse.Success, $"GetEmail failed: {getResponse.Message}");
            Assert.True(getResponse.Emails.Count > 0, "No emails available to move");

            var email = getResponse.Emails[0];
            Console.WriteLine($"Moving email: {email.Subject} to label {folderName}");

            // Move the email using the label ID returned from CreateFolder
            var moved = await _gmailService.MoveEmailToFolder(email.Id, createResponse.ServiceSpecificId!);

            Assert.True(moved, "MoveEmailToFolder should return true on success");
        }

        public void Dispose()
        {
            _loggerFactory?.Dispose();
        }
    }
}
