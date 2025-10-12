using System.Text.Json.Serialization;

namespace EmailAgent.Entities;

/// <summary>
/// Represents the type of email folder
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FolderType
{
    /// <summary>
    /// Inbox folder - primary incoming emails
    /// </summary>
    Inbox,

    /// <summary>
    /// Sent folder - emails that have been sent
    /// </summary>
    Sent,

    /// <summary>
    /// Drafts folder - unsent draft emails
    /// </summary>
    Drafts,

    /// <summary>
    /// Spam/Junk folder - emails flagged as spam
    /// </summary>
    Spam,

    /// <summary>
    /// Trash/Deleted Items folder - deleted emails
    /// </summary>
    Trash,

    /// <summary>
    /// Custom folder - user-defined or service-specific folder
    /// </summary>
    Custom
}