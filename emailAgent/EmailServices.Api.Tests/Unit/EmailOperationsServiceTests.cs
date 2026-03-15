using EmailServices.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace EmailServices.Api.Tests.Unit;

public class EmailOperationsServiceTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<EmailOperationsService>> _mockLogger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly EmailOperationsService _emailService;

    public EmailOperationsServiceTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<EmailOperationsService>>();

        // Use the actual LoggerFactory instead of mocking
        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

        // Setup basic configuration
        _mockConfiguration.Setup(c => c["keyvaultName"]).Returns("kvGPSecrets");

        _emailService = new EmailOperationsService(_mockConfiguration.Object, _mockLogger.Object, _loggerFactory);
    }

    [Fact]
    public async Task DeleteEmailAsync_WithInvalidService_ShouldThrowArgumentException()
    {
        // Arrange
        var emailId = "test-email-id";
        var service = "InvalidService";
        var userEmail = "test@gmail.com";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _emailService.DeleteEmailAsync(emailId, service, userEmail));

        exception.Message.Should().Contain("Invalid email service");
    }

    [Fact]
    public async Task MoveEmailAsync_WithGmail_ShouldThrowNotImplementedException()
    {
        // Arrange
        var emailId = "test-email-id";
        var service = "Gmail";
        var userEmail = "test@gmail.com";
        var folder = "TestFolder";

        // Act & Assert
        await Assert.ThrowsAsync<NotImplementedException>(async () =>
            await _emailService.MoveEmailAsync(emailId, service, userEmail, folder));
    }

    [Fact]
    public async Task MoveEmailAsync_WithOutlook_ShouldThrowNotImplementedException()
    {
        // Arrange
        var emailId = "test-email-id";
        var service = "Outlook";
        var userEmail = "test@outlook.com";
        var folder = "TestFolder";

        // Act & Assert
        await Assert.ThrowsAsync<NotImplementedException>(async () =>
            await _emailService.MoveEmailAsync(emailId, service, userEmail, folder));
    }

    [Theory]
    [InlineData("gmail")]
    [InlineData("GMAIL")]
    [InlineData("Gmail")]
    [InlineData("outlook")]
    [InlineData("OUTLOOK")]
    [InlineData("Outlook")]
    public async Task DeleteEmailAsync_WithValidServiceNames_ShouldParseCorrectly(string serviceName)
    {
        // Arrange
        var emailId = "test-email-id";
        var userEmail = "test@example.com";

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
            await _emailService.DeleteEmailAsync(emailId, serviceName, userEmail));

        // Should not throw argument validation errors for service name
        exception.Should().NotBeOfType<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new EmailOperationsService(null!, _mockLogger.Object, _loggerFactory));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new EmailOperationsService(_mockConfiguration.Object, null!, _loggerFactory));
    }
}
