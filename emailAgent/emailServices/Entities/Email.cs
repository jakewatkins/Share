using System.Text.Json.Serialization;

namespace EmailAgent.Entities;

/// <summary>
/// Represents an email message
/// </summary>
public class Email
{
    /// <summary>
    /// The unique identifier for the email message (service-specific)
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The email service that retrieved this email
    /// </summary>
    [JsonPropertyName("service")]
    public EmailService Service { get; set; }

    /// <summary>
    /// The sender's email address
    /// </summary>
    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// Collection of recipient email addresses
    /// </summary>
    [JsonPropertyName("to")]
    public List<string> To { get; set; } = new();

    /// <summary>
    /// Collection of CC recipient email addresses
    /// </summary>
    [JsonPropertyName("cc")]
    public List<string> CC { get; set; } = new();

    /// <summary>
    /// Collection of BCC recipient email addresses
    /// </summary>
    [JsonPropertyName("bcc")]
    public List<string> BCC { get; set; } = new();

    /// <summary>
    /// The date and time the email was sent
    /// </summary>
    [JsonPropertyName("sentDateTime")]
    public DateTime SentDateTime { get; set; }

    /// <summary>
    /// The subject of the email
    /// </summary>
    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// The body of the email message. HTML is preferred over plain text.
    /// </summary>
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Collection of email attachments
    /// </summary>
    [JsonPropertyName("attachments")]
    public List<EmailAttachment> Attachments { get; set; } = new();
}
