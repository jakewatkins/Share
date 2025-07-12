using System.Text.Json.Serialization;

namespace EmailAgent.Entities;

/// <summary>
/// Represents an email attachment
/// </summary>
public class EmailAttachment
{
    /// <summary>
    /// The filename of the attachment
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The MIME type of the attachment
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The size of the attachment in bytes
    /// </summary>
    [JsonPropertyName("size")]
    public long Size { get; set; }

    /// <summary>
    /// The base64 encoded content of the attachment. 
    /// Null if the attachment was too large to download.
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}
