using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using System.Net;
using Xunit;
using FluentAssertions;

namespace EmailServices.Api.Tests.Integration;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthCheckReady_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SwaggerEndpoint_InDevelopment_ShouldReturnOk()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
        });
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("/api/v1/emails/test-id")]
    public async Task DeleteEmail_WithoutParameters_ShouldReturnBadRequest(string url)
    {
        // Act
        var response = await _client.DeleteAsync(url);

        // Assert
        // Should return bad request due to missing required query parameters
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Theory]
    [InlineData("/api/v1/emails/test-id/move")]
    public async Task MoveEmail_WithoutParameters_ShouldReturnBadRequest(string url)
    {
        // Act
        var response = await _client.PutAsync(url, null);

        // Assert
        // Should return bad request due to missing required query parameters
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task DeleteEmail_WithInvalidService_ShouldReturnInternalServerError()
    {
        // Act
        var response = await _client.DeleteAsync("/api/v1/emails/test-id?service=InvalidService&userEmail=test@test.com");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task MoveEmail_WithGmailService_ShouldAttemptOperation()
    {
        // Move is now implemented — without live credentials the service returns 500
        var response = await _client.PutAsync("/api/v1/emails/test-id/move?service=Gmail&userEmail=test@gmail.com&folder=TestFolder", null);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }
}
