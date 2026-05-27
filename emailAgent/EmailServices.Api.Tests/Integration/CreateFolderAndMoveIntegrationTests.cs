using EmailAgent.Entities;
using EmailServices.Api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;
using System.Text.Json;
using Xunit;
using FluentAssertions;

namespace EmailServices.Api.Tests.Integration;

public class CreateFolderAndMoveIntegrationTests
{
    private readonly Mock<IEmailOperationsService> _mockEmailService;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CreateFolderAndMoveIntegrationTests()
    {
        _mockEmailService = new Mock<IEmailOperationsService>();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IEmailOperationsService));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddScoped<IEmailOperationsService>(_ => _mockEmailService.Object);
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateFolder_Gmail_ShouldReturnOkWithFolderDetails()
    {
        _mockEmailService
            .Setup(s => s.CreateFolderAsync("Gmail", "jake.watkins@gmail.com", "Newsletters"))
            .ReturnsAsync(new CreateFolderResponse
            {
                Success = true,
                FolderName = "Newsletters",
                ServiceSpecificId = "Label_12345",
                Service = EmailService.Gmail
            });

        var response = await _client.PostAsync(
            "/api/v1/folders?service=Gmail&userEmail=jake.watkins@gmail.com&folderName=Newsletters", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        json.RootElement.GetProperty("data").GetProperty("folderName").GetString().Should().Be("Newsletters");
        json.RootElement.GetProperty("data").GetProperty("serviceSpecificId").GetString().Should().Be("Label_12345");

        _mockEmailService.Verify(s => s.CreateFolderAsync("Gmail", "jake.watkins@gmail.com", "Newsletters"), Times.Once);
    }

    [Fact]
    public async Task CreateFolder_Outlook_ShouldReturnOkWithFolderDetails()
    {
        _mockEmailService
            .Setup(s => s.CreateFolderAsync("Outlook", "jake.watkins@outlook.com", "Recruiters"))
            .ReturnsAsync(new CreateFolderResponse
            {
                Success = true,
                FolderName = "Recruiters",
                ServiceSpecificId = "AAMkAGFolder123==",
                Service = EmailService.Outlook
            });

        var response = await _client.PostAsync(
            "/api/v1/folders?service=Outlook&userEmail=jake.watkins@outlook.com&folderName=Recruiters", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        json.RootElement.GetProperty("data").GetProperty("folderName").GetString().Should().Be("Recruiters");
        json.RootElement.GetProperty("data").GetProperty("serviceSpecificId").GetString().Should().Be("AAMkAGFolder123==");

        _mockEmailService.Verify(s => s.CreateFolderAsync("Outlook", "jake.watkins@outlook.com", "Recruiters"), Times.Once);
    }

    [Fact]
    public async Task CreateFolder_WithInvalidService_ShouldReturnBadRequest()
    {
        _mockEmailService
            .Setup(s => s.CreateFolderAsync("InvalidService", It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new ArgumentException("Invalid email service: InvalidService"));

        var response = await _client.PostAsync(
            "/api/v1/folders?service=InvalidService&userEmail=test@example.com&folderName=Test", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MoveEmail_Gmail_ShouldReturnOk()
    {
        _mockEmailService
            .Setup(s => s.MoveEmailAsync("gmail-msg-001", "Gmail", "jake.watkins@gmail.com", "Newsletters"))
            .ReturnsAsync(true);

        var response = await _client.PutAsync(
            "/api/v1/emails/gmail-msg-001/move?service=Gmail&userEmail=jake.watkins@gmail.com&folder=Newsletters", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        json.RootElement.GetProperty("data").GetProperty("moved").GetBoolean().Should().BeTrue();
        json.RootElement.GetProperty("data").GetProperty("folder").GetString().Should().Be("Newsletters");

        _mockEmailService.Verify(s => s.MoveEmailAsync("gmail-msg-001", "Gmail", "jake.watkins@gmail.com", "Newsletters"), Times.Once);
    }

    [Fact]
    public async Task MoveEmail_Outlook_ShouldReturnOk()
    {
        _mockEmailService
            .Setup(s => s.MoveEmailAsync("outlook-aabbcc", "Outlook", "jake.watkins@outlook.com", "Recruiters"))
            .ReturnsAsync(true);

        var response = await _client.PutAsync(
            "/api/v1/emails/outlook-aabbcc/move?service=Outlook&userEmail=jake.watkins@outlook.com&folder=Recruiters", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        json.RootElement.GetProperty("data").GetProperty("moved").GetBoolean().Should().BeTrue();

        _mockEmailService.Verify(s => s.MoveEmailAsync("outlook-aabbcc", "Outlook", "jake.watkins@outlook.com", "Recruiters"), Times.Once);
    }

    [Fact]
    public async Task CreateFolder_ThenMoveEmail_Gmail_ShouldSucceed()
    {
        // Arrange
        var email = new Email { Id = "gmail-msg-001", Service = EmailService.Gmail, Subject = "Job offer" };

        _mockEmailService
            .Setup(s => s.GetEmailsAsync("Gmail", "jake.watkins@gmail.com", 10))
            .ReturnsAsync(new GetEmailResponse
            {
                Success = true,
                Count = 1,
                Emails = [email],
                Service = EmailService.Gmail
            });

        _mockEmailService
            .Setup(s => s.CreateFolderAsync("Gmail", "jake.watkins@gmail.com", "Recruiters"))
            .ReturnsAsync(new CreateFolderResponse
            {
                Success = true,
                FolderName = "Recruiters",
                ServiceSpecificId = "Label_99999",
                Service = EmailService.Gmail
            });

        _mockEmailService
            .Setup(s => s.MoveEmailAsync("gmail-msg-001", "Gmail", "jake.watkins@gmail.com", "Label_99999"))
            .ReturnsAsync(true);

        // Act — get emails
        var getResponse = await _client.GetAsync(
            "/api/v1/emails?service=Gmail&userEmail=jake.watkins@gmail.com&count=10");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getJson = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        var emailId = getJson.RootElement.GetProperty("data").GetProperty("emails")[0].GetProperty("id").GetString();
        emailId.Should().Be("gmail-msg-001");

        // Act — create folder
        var createResponse = await _client.PostAsync(
            "/api/v1/folders?service=Gmail&userEmail=jake.watkins@gmail.com&folderName=Recruiters", null);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var createJson = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var labelId = createJson.RootElement.GetProperty("data").GetProperty("serviceSpecificId").GetString();
        labelId.Should().Be("Label_99999");

        // Act — move email using the label ID
        var moveResponse = await _client.PutAsync(
            $"/api/v1/emails/{emailId}/move?service=Gmail&userEmail=jake.watkins@gmail.com&folder={labelId}", null);
        moveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var moveJson = JsonDocument.Parse(await moveResponse.Content.ReadAsStringAsync());
        moveJson.RootElement.GetProperty("data").GetProperty("moved").GetBoolean().Should().BeTrue();

        // Verify all three operations were called
        _mockEmailService.Verify(s => s.GetEmailsAsync("Gmail", "jake.watkins@gmail.com", 10), Times.Once);
        _mockEmailService.Verify(s => s.CreateFolderAsync("Gmail", "jake.watkins@gmail.com", "Recruiters"), Times.Once);
        _mockEmailService.Verify(s => s.MoveEmailAsync("gmail-msg-001", "Gmail", "jake.watkins@gmail.com", "Label_99999"), Times.Once);
    }
}
