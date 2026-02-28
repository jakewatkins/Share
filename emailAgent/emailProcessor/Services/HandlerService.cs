using Azure.Data.Tables;
using EmailProcessor.Models;
using Microsoft.Extensions.Logging;

namespace EmailProcessor.Services;

/// <summary>
/// Service for handling HTTP posts to email handlers
/// </summary>
public interface IHandlerService
{
    /// <summary>
    /// Posts an email to the specified handler URL
    /// </summary>
    /// <param name="email">The email to post</param>
    /// <param name="handlerUrl">The URL to post to</param>
    /// <returns>Task representing the async operation</returns>
    Task PostEmailAsync(ProcessedEmail email, string handlerUrl);

    /// <summary>
    /// Gets the handler URL for an email from the whitelist
    /// </summary>
    /// <param name="fromAddress">The email's from address</param>
    /// <returns>EmailWhiteListEntity if found, null otherwise</returns>
    Task<EmailWhiteListEntity?> GetEmailWhiteListEntryAsync(string fromAddress);

    /// <summary>
    /// Gets the handler URL for a category
    /// </summary>
    /// <param name="category">The email category/classification</param>
    /// <returns>CategoryHandlerEntity if found, null otherwise</returns>
    Task<CategoryHandlerEntity?> GetCategoryHandlerAsync(string category);
}

/// <summary>
/// Implementation of handler service for posting emails to handler URLs
/// </summary>
public class HandlerService : IHandlerService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HandlerService> _logger;
    private readonly TableServiceClient _tableServiceClient;
    private readonly string _apiKey;

    /// <summary>
    /// Initializes a new instance of the HandlerService
    /// </summary>
    /// <param name="httpClient">HTTP client for making requests</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="tableServiceClient">Azure Table service client</param>
    /// <param name="apiKey">API key for authentication</param>
    public HandlerService(HttpClient httpClient, ILogger<HandlerService> logger, TableServiceClient tableServiceClient, string apiKey)
    {
        _httpClient = httpClient;
        _logger = logger;
        _tableServiceClient = tableServiceClient;
        _apiKey = apiKey;
    }

    /// <summary>
    /// Posts an email to the specified handler URL
    /// </summary>
    /// <param name="email">The email to post</param>
    /// <param name="handlerUrl">The URL to post to</param>
    public async Task PostEmailAsync(ProcessedEmail email, string handlerUrl)
    {
        try
        {
            _logger.LogInformation("Posting email {EmailId} to handler {HandlerUrl}", email.Id, handlerUrl);

            // Serialize the email to JSON
            var json = System.Text.Json.JsonSerializer.Serialize(email);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            // Add API key header
            content.Headers.Add("API-KEY", _apiKey);

            // Make the HTTP POST request
            var response = await _httpClient.PostAsync(handlerUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorMessage = $"Error posting email to handler. " +
                                 $"HTTP Status Code: {response.StatusCode}, " +
                                 $"Response Body: {errorContent}, " +
                                 $"Email ID: {email.Id}, " +
                                 $"Handler URL: {handlerUrl}";

                _logger.LogError(errorMessage);
                throw new HttpRequestException(errorMessage);
            }

            _logger.LogInformation("Successfully posted email {EmailId} to handler {HandlerUrl}", email.Id, handlerUrl);
        }
        catch (Exception ex)
        {
            var errorMessage = $"Exception occurred posting email to handler. " +
                             $"Email ID: {email.Id}, " +
                             $"Handler URL: {handlerUrl}, " +
                             $"Exception: {ex.Message}";

            _logger.LogError(ex, errorMessage);
            throw new InvalidOperationException(errorMessage, ex);
        }
    }

    /// <summary>
    /// Gets the handler URL for an email from the whitelist
    /// </summary>
    /// <param name="fromAddress">The email's from address</param>
    /// <returns>EmailWhiteListEntity if found, null otherwise</returns>
    public async Task<EmailWhiteListEntity?> GetEmailWhiteListEntryAsync(string fromAddress)
    {
        try
        {
            var tableClient = _tableServiceClient.GetTableClient("emailWhiteList");

            // Query for the email address
            var query = tableClient.QueryAsync<EmailWhiteListEntity>(
                filter: $"PartitionKey eq 'DigitalValet' and email eq '{fromAddress}'");

            await foreach (var entity in query)
            {
                _logger.LogInformation("Found whitelist entry for email {EmailAddress}", fromAddress);
                return entity;
            }

            _logger.LogDebug("No whitelist entry found for email {EmailAddress}", fromAddress);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying EmailWhiteList for address {EmailAddress}", fromAddress);
            throw;
        }
    }

    /// <summary>
    /// Gets the handler URL for a category
    /// </summary>
    /// <param name="category">The email category/classification</param>
    /// <returns>CategoryHandlerEntity if found, null otherwise</returns>
    public async Task<CategoryHandlerEntity?> GetCategoryHandlerAsync(string category)
    {
        try
        {
            var tableClient = _tableServiceClient.GetTableClient("categoryHandler");

            // Query for the category
            var query = tableClient.QueryAsync<CategoryHandlerEntity>(
                filter: $"PartitionKey eq 'DigitalValet' and category eq '{category}'");

            await foreach (var entity in query)
            {
                _logger.LogInformation("Found category handler for category {Category}", category);
                return entity;
            }

            _logger.LogDebug("No category handler found for category {Category}", category);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying CategoryHandler for category {Category}", category);
            throw;
        }
    }
}
