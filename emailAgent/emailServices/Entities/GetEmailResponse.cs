using System.Text.Json.Serialization;

namespace EmailAgent.Entities;

/// <summary>
/// Response object for email retrieval operations
/// </summary>
public class GetEmailResponse
{
    /// <summary>
    /// Indicates whether the email retrieval operation was successful
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Status message or error details if Success is false
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The number of emails retrieved
    /// </summary>
    [JsonPropertyName("count")]
    public int Count { get; set; }

    /// <summary>
    /// Collection of retrieved emails
    /// </summary>
    [JsonPropertyName("emails")]
    public List<Email> Emails { get; set; } = new();

    /// <summary>
    /// The email service that processed this request
    /// </summary>
    [JsonPropertyName("service")]
    public EmailService Service { get; set; }
}
