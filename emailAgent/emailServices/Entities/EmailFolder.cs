using System.Text.Json.Serialization;

namespace EmailAgent.Entities;

/// <summary>
/// Represents an email folder with service-specific mapping capabilities
/// </summary>
public class EmailFolder
{
    /// <summary>
    /// The logical name of the folder (used for display and cross-service consistency)
    /// </summary>
    public string FolderName { get; set; } = string.Empty;

    /// <summary>
    /// The type of folder this represents
    /// </summary>
    public FolderType FolderType { get; set; } = FolderType.Inbox;

    /// <summary>
    /// Service-specific identifier for this folder (e.g., "INBOX" for Gmail, folder ID for Outlook)
    /// This allows each service to map to its own internal folder structure
    /// </summary>
    public string? ServiceSpecificId { get; set; }

    /// <summary>
    /// The email service this folder belongs to
    /// </summary>
    public EmailService Service { get; set; } = EmailService.Gmail;

    /// <summary>
    /// Creates a new EmailFolder instance
    /// </summary>
    public EmailFolder() { }

    /// <summary>
    /// Creates a new EmailFolder with the specified properties
    /// </summary>
    /// <param name="folderName">The logical name of the folder</param>
    /// <param name="folderType">The type of folder</param>
    /// <param name="service">The email service this folder belongs to</param>
    /// <param name="serviceSpecificId">Optional service-specific identifier</param>
    public EmailFolder(string folderName, FolderType folderType, EmailService service, string? serviceSpecificId = null)
    {
        FolderName = folderName;
        FolderType = folderType;
        Service = service;
        ServiceSpecificId = serviceSpecificId;
    }

    /// <summary>
    /// Creates a standard Inbox folder for the specified service
    /// </summary>
    /// <param name="service">The email service</param>
    /// <returns>A new EmailFolder configured for Inbox</returns>
    public static EmailFolder CreateInboxFolder(EmailService service)
    {
        return service switch
        {
            EmailService.Gmail => new EmailFolder("Inbox", FolderType.Inbox, service, "INBOX"),
            EmailService.Outlook => new EmailFolder("Inbox", FolderType.Inbox, service, "Inbox"),
            EmailService.OWA => new EmailFolder("Inbox", FolderType.Inbox, service, "WellKnownFolderName.Inbox"),
            _ => new EmailFolder("Inbox", FolderType.Inbox, service)
        };
    }

    /// <summary>
    /// Creates a standard Spam folder for the specified service
    /// </summary>
    /// <param name="service">The email service</param>
    /// <returns>A new EmailFolder configured for Spam</returns>
    public static EmailFolder CreateSpamFolder(EmailService service)
    {
        return service switch
        {
            EmailService.Gmail => new EmailFolder("Spam", FolderType.Spam, service, "SPAM"),
            EmailService.Outlook => new EmailFolder("Junk Email", FolderType.Spam, service, "JunkEmail"),
            EmailService.OWA => new EmailFolder("Junk Email", FolderType.Spam, service, "WellKnownFolderName.JunkEmail"),
            _ => new EmailFolder("Spam", FolderType.Spam, service)
        };
    }

    /// <summary>
    /// Creates a standard Sent folder for the specified service
    /// </summary>
    /// <param name="service">The email service</param>
    /// <returns>A new EmailFolder configured for Sent items</returns>
    public static EmailFolder CreateSentFolder(EmailService service)
    {
        return service switch
        {
            EmailService.Gmail => new EmailFolder("Sent", FolderType.Sent, service, "SENT"),
            EmailService.Outlook => new EmailFolder("Sent Items", FolderType.Sent, service, "SentItems"),
            EmailService.OWA => new EmailFolder("Sent Items", FolderType.Sent, service, "WellKnownFolderName.SentItems"),
            _ => new EmailFolder("Sent", FolderType.Sent, service)
        };
    }

    /// <summary>
    /// Creates a standard Drafts folder for the specified service
    /// </summary>
    /// <param name="service">The email service</param>
    /// <returns>A new EmailFolder configured for Drafts</returns>
    public static EmailFolder CreateDraftsFolder(EmailService service)
    {
        return service switch
        {
            EmailService.Gmail => new EmailFolder("Drafts", FolderType.Drafts, service, "DRAFT"),
            EmailService.Outlook => new EmailFolder("Drafts", FolderType.Drafts, service, "Drafts"),
            EmailService.OWA => new EmailFolder("Drafts", FolderType.Drafts, service, "WellKnownFolderName.Drafts"),
            _ => new EmailFolder("Drafts", FolderType.Drafts, service)
        };
    }

    /// <summary>
    /// Creates a standard Trash folder for the specified service
    /// </summary>
    /// <param name="service">The email service</param>
    /// <returns>A new EmailFolder configured for Trash/Deleted items</returns>
    public static EmailFolder CreateTrashFolder(EmailService service)
    {
        return service switch
        {
            EmailService.Gmail => new EmailFolder("Trash", FolderType.Trash, service, "TRASH"),
            EmailService.Outlook => new EmailFolder("Deleted Items", FolderType.Trash, service, "DeletedItems"),
            EmailService.OWA => new EmailFolder("Deleted Items", FolderType.Trash, service, "WellKnownFolderName.DeletedItems"),
            _ => new EmailFolder("Trash", FolderType.Trash, service)
        };
    }

    /// <summary>
    /// Returns a string representation of the folder
    /// </summary>
    public override string ToString()
    {
        return $"{FolderName} ({FolderType}) - {Service}";
    }
}