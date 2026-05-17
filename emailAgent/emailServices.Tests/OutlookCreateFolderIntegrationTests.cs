using EmailAgent.Core;
using EmailAgent.Entities;
using EmailAgent.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace emailServices.Tests
{
    public class OutlookCreateFolderIntegrationTests : IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly ILoggerFactory _loggerFactory;
        private readonly OutlookService _outlookService;

        public OutlookCreateFolderIntegrationTests()
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("settings.json", optional: false, reloadOnChange: true)
                .Build();

            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            var keyVaultLogger = _loggerFactory.CreateLogger<KeyVaultService>();
            var keyVaultService = new KeyVaultService(_configuration, keyVaultLogger);

            var agentConfig = new AgentConfiguration(_configuration);
            var logger = _loggerFactory.CreateLogger<OutlookService>();
            _outlookService = new OutlookService(agentConfig, keyVaultService, logger, "test@outlook.com");
        }

        [Fact]
        public async Task CreateFolder_WithValidName_ShouldReturnSuccessWithId()
        {
            var folderName = $"TestFolder-{DateTime.UtcNow:yyyyMMddHHmmss}";

            var response = await _outlookService.CreateFolder(folderName);

            Assert.True(response.Success, $"CreateFolder failed: {response.Message}");
            Assert.Equal(EmailService.Outlook, response.Service);
            Assert.Equal(folderName, response.FolderName);
            Assert.NotNull(response.ServiceSpecificId);
            Assert.NotEmpty(response.ServiceSpecificId);

            Console.WriteLine($"Created Outlook folder: {response.FolderName} (ID: {response.ServiceSpecificId})");
        }

        [Fact]
        public async Task MoveEmailToFolder_ShouldSucceed()
        {
            // Create a test folder to move into
            var folderName = $"MoveTest-{DateTime.UtcNow:yyyyMMddHHmmss}";
            var createResponse = await _outlookService.CreateFolder(folderName);
            Assert.True(createResponse.Success, $"CreateFolder failed: {createResponse.Message}");

            // Fetch one email from inbox
            var getRequest = new GetEmailRequest
            {
                NumberOfEmails = 1,
                Folder = EmailFolder.CreateInboxFolder(EmailService.Outlook)
            };
            var getResponse = await _outlookService.GetEmail(getRequest);
            Assert.True(getResponse.Success, $"GetEmail failed: {getResponse.Message}");
            Assert.True(getResponse.Emails.Count > 0, "No emails available to move");

            var email = getResponse.Emails[0];
            Console.WriteLine($"Moving email: {email.Subject} to folder {folderName}");

            // Move the email using the folder ID returned from CreateFolder
            var moved = await _outlookService.MoveEmailToFolder(email.Id, createResponse.ServiceSpecificId!);

            Assert.True(moved, "MoveEmailToFolder should return true on success");
        }

        public void Dispose()
        {
            _loggerFactory?.Dispose();
        }
    }
}
