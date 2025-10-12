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

    /// <summary>
    /// The email folder to retrieve emails from. If null, defaults to Inbox for backward compatibility.
    /// </summary>
    [JsonPropertyName("folder")]
    public EmailFolder? Folder { get; set; }

    /// <summary>
    /// Creates a new GetEmailRequest instance
    /// </summary>
    public GetEmailRequest() { }

    /// <summary>
    /// Creates a new GetEmailRequest with the specified parameters
    /// </summary>
    /// <param name="startIndex">The starting index for email retrieval (0-based)</param>
    /// <param name="numberOfEmails">The number of emails to retrieve</param>
    /// <param name="folder">Optional folder to retrieve emails from (defaults to Inbox if null)</param>
    public GetEmailRequest(int startIndex, int numberOfEmails, EmailFolder? folder = null)
    {
        StartIndex = startIndex;
        NumberOfEmails = numberOfEmails;
        Folder = folder;
    }

    /// <summary>
    /// Gets the effective folder for the request, defaulting to Inbox for the specified service if no folder is set
    /// </summary>
    /// <param name="service">The email service to get the default folder for</param>
    /// <returns>The folder to use for the request</returns>
    public EmailFolder GetEffectiveFolder(EmailService service)
    {
        return Folder ?? EmailFolder.CreateInboxFolder(service);
    }
}
