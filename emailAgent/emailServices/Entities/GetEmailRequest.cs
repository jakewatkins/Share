using System.Text.Json.Serialization;

namespace EmailAgent.Entities;

/// <summary>
/// Request object for retrieving emails from any email service
/// </summary>
public class GetEmailRequest
{
    /// <summary>
    /// The starting index for email retrieval (0-based)
    /// </summary>
    [JsonPropertyName("startIndex")]
    public int StartIndex { get; set; }

    /// <summary>
    /// The number of emails to retrieve
    /// </summary>
    [JsonPropertyName("numberOfEmails")]
    public int NumberOfEmails { get; set; }
}
