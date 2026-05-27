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

public class RetrieveAndDeleteIntegrationTests
{
    private readonly Mock<IEmailOperationsService> _mockEmailService;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public RetrieveAndDeleteIntegrationTests()
    {
        _mockEmailService = new Mock<IEmailOperationsService>();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace real service with mock so tests don't need live credentials
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
    public async Task GetEmails_ThenDeleteFirstEmail_Gmail_ShouldSucceed()
    {
        // Arrange - set up two emails to be returned
        var email1 = new Email { Id = "gmail-msg-001", Service = EmailService.Gmail, From = "sender@example.com", Subject = "Test Email 1" };
        var email2 = new Email { Id = "gmail-msg-002", Service = EmailService.Gmail, From = "other@example.com", Subject = "Test Email 2" };

        var getResponse = new GetEmailResponse
        {
            Success = true,
            Message = "OK",
            Count = 2,
            Emails = [email1, email2],
            Service = EmailService.Gmail
        };

        _mockEmailService
            .Setup(s => s.GetEmailsAsync("Gmail", "jake.watkins@gmail.com", 10))
            .ReturnsAsync(getResponse);

        _mockEmailService
            .Setup(s => s.DeleteEmailAsync("gmail-msg-001", "Gmail", "jake.watkins@gmail.com"))
            .ReturnsAsync(true);

        // Act - step 1: retrieve emails
        var getHttpResponse = await _client.GetAsync(
            "/api/v1/emails?service=Gmail&userEmail=jake.watkins@gmail.com&count=10");

        // Assert - GET succeeded
        getHttpResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getBody = await getHttpResponse.Content.ReadAsStringAsync();
        var getJson = JsonDocument.Parse(getBody);
        getJson.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();

        var emailsArray = getJson.RootElement
            .GetProperty("data")
            .GetProperty("emails");
        emailsArray.GetArrayLength().Should().Be(2);

        // Extract the first email ID from the response
        var firstEmailId = emailsArray[0].GetProperty("id").GetString();
        firstEmailId.Should().Be("gmail-msg-001");

        // Act - step 2: delete that email via the API
        var deleteHttpResponse = await _client.DeleteAsync(
            $"/api/v1/emails/{firstEmailId}?service=Gmail&userEmail=jake.watkins@gmail.com");

        // Assert - DELETE succeeded
        deleteHttpResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteBody = await deleteHttpResponse.Content.ReadAsStringAsync();
        var deleteJson = JsonDocument.Parse(deleteBody);
        deleteJson.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        deleteJson.RootElement.GetProperty("data").GetProperty("deleted").GetBoolean().Should().BeTrue();

        // Verify both service methods were called exactly once
        _mockEmailService.Verify(s => s.GetEmailsAsync("Gmail", "jake.watkins@gmail.com", 10), Times.Once);
        _mockEmailService.Verify(s => s.DeleteEmailAsync("gmail-msg-001", "Gmail", "jake.watkins@gmail.com"), Times.Once);
    }

    [Fact]
    public async Task GetEmails_ThenDeleteFirstEmail_Outlook_ShouldSucceed()
    {
        // Arrange
        var email = new Email { Id = "outlook-aabbcc", Service = EmailService.Outlook, From = "boss@company.com", Subject = "Q1 Review" };

        var getResponse = new GetEmailResponse
        {
            Success = true,
            Message = "OK",
            Count = 1,
            Emails = [email],
            Service = EmailService.Outlook
        };

        _mockEmailService
            .Setup(s => s.GetEmailsAsync("Outlook", "jake.watkins@outlook.com", 10))
            .ReturnsAsync(getResponse);

        _mockEmailService
            .Setup(s => s.DeleteEmailAsync("outlook-aabbcc", "Outlook", "jake.watkins@outlook.com"))
            .ReturnsAsync(true);

        // Act - retrieve
        var getHttpResponse = await _client.GetAsync(
            "/api/v1/emails?service=Outlook&userEmail=jake.watkins@outlook.com&count=10");

        getHttpResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getJson = JsonDocument.Parse(await getHttpResponse.Content.ReadAsStringAsync());
        var firstEmailId = getJson.RootElement
            .GetProperty("data")
            .GetProperty("emails")[0]
            .GetProperty("id")
            .GetString();

        firstEmailId.Should().Be("outlook-aabbcc");

        // Act - delete
        var deleteHttpResponse = await _client.DeleteAsync(
            $"/api/v1/emails/{firstEmailId}?service=Outlook&userEmail=jake.watkins@outlook.com");

        // Assert
        deleteHttpResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteJson = JsonDocument.Parse(await deleteHttpResponse.Content.ReadAsStringAsync());
        deleteJson.RootElement.GetProperty("data").GetProperty("deleted").GetBoolean().Should().BeTrue();

        _mockEmailService.Verify(s => s.GetEmailsAsync("Outlook", "jake.watkins@outlook.com", 10), Times.Once);
        _mockEmailService.Verify(s => s.DeleteEmailAsync("outlook-aabbcc", "Outlook", "jake.watkins@outlook.com"), Times.Once);
    }

    [Fact]
    public async Task GetEmails_WithInvalidService_ShouldReturnBadRequest()
    {
        _mockEmailService
            .Setup(s => s.GetEmailsAsync("InvalidService", It.IsAny<string>(), It.IsAny<int>()))
            .ThrowsAsync(new ArgumentException("Invalid email service: InvalidService"));

        var response = await _client.GetAsync(
            "/api/v1/emails?service=InvalidService&userEmail=test@example.com&count=10");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetEmails_WhenServiceReturnsEmpty_DeleteShouldNotBeCalled()
    {
        // Arrange - empty inbox
        _mockEmailService
            .Setup(s => s.GetEmailsAsync("Gmail", "empty@gmail.com", 10))
            .ReturnsAsync(new GetEmailResponse { Success = true, Count = 0, Emails = [], Service = EmailService.Gmail });

        // Act
        var response = await _client.GetAsync(
            "/api/v1/emails?service=Gmail&userEmail=empty@gmail.com&count=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("data").GetProperty("count").GetInt32().Should().Be(0);

        // Delete was never called
        _mockEmailService.Verify(s => s.DeleteEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
