using System.Net;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace EmailServices.Api.Tests.Integration;

/// <summary>
/// End-to-end tests against the live deployed API.
/// Requires the service to be running. Set EMAIL_API_BASE_URL to override the default.
/// Example: EMAIL_API_BASE_URL=http://192.168.1.10:5005 dotnet test --filter "Category=Live"
/// </summary>
[Trait("Category", "Live")]
public class LiveApiEndToEndTests : IDisposable
{
    private readonly HttpClient _client;
    private readonly string _userEmail;
    private readonly string _service;

    public LiveApiEndToEndTests()
    {
        var baseUrl = Environment.GetEnvironmentVariable("EMAIL_API_BASE_URL") ?? "http://localhost:5005";
        _userEmail = Environment.GetEnvironmentVariable("EMAIL_API_USER") ?? "jake.watkins@gmail.com";
        _service = Environment.GetEnvironmentVariable("EMAIL_API_SERVICE") ?? "Gmail";

        _client = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    [Fact]
    public async Task GetEmails_ThenDeleteFirstEmail_ShouldSucceed()
    {
        // Step 1: retrieve emails
        var getResponse = await _client.GetAsync(
            $"/api/v1/emails?service={_service}&userEmail={_userEmail}&count=1");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "GET /api/v1/emails should return 200");

        var getBody = await getResponse.Content.ReadAsStringAsync();
        var getJson = JsonDocument.Parse(getBody);

        getJson.RootElement.GetProperty("success").GetBoolean().Should().BeTrue(
            $"API response indicated failure: {getBody}");

        var data = getJson.RootElement.GetProperty("data");
        data.GetProperty("success").GetBoolean().Should().BeTrue(
            $"Email service authentication or retrieval failed: {data.GetProperty("message").GetString()}");

        var emails = data.GetProperty("emails");
        emails.GetArrayLength().Should().BeGreaterThan(0,
            "inbox must have at least one email to delete");

        var firstEmail = emails[0];
        var emailId = firstEmail.GetProperty("id").GetString()!;
        var subject = firstEmail.TryGetProperty("subject", out var subj) ? subj.GetString() : "(no subject)";
        var from = firstEmail.TryGetProperty("from", out var f) ? f.GetString() : "(unknown)";

        Console.WriteLine($"Deleting - From: {from} | Subject: {subject} | Id: {emailId}");

        // Step 2: delete that email via the API
        var deleteResponse = await _client.DeleteAsync(
            $"/api/v1/emails/{emailId}?service={_service}&userEmail={_userEmail}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "DELETE /api/v1/emails/{id} should return 200");

        var deleteBody = await deleteResponse.Content.ReadAsStringAsync();
        var deleteJson = JsonDocument.Parse(deleteBody);

        deleteJson.RootElement.GetProperty("success").GetBoolean().Should().BeTrue(
            $"Delete response indicated failure: {deleteBody}");
        deleteJson.RootElement.GetProperty("data").GetProperty("deleted").GetBoolean().Should().BeTrue();
    }

    public void Dispose() => _client.Dispose();
}
